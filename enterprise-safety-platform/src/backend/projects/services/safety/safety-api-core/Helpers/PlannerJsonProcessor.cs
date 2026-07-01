using System.Text.Json;

namespace Platform.Legacy.Core.Helpers;

public static class PlannerJsonProcessor
{
	public static string ProcessSchemaJson(string? schemaJson)
	{
		if (string.IsNullOrEmpty(schemaJson))
		{
			return schemaJson ?? "";
		}

		JsonElement schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
		string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

		// Check if this is v1 format (object with version property)
		if (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty(propertyName: "version", value: out JsonElement versionProp))
		{
			// V1 format - extract tasks array, process it, and rebuild the object
			if (
				!schema.TryGetProperty(propertyName: "tasks", value: out JsonElement tasksElement)
				|| tasksElement.ValueKind != JsonValueKind.Array
			)
			{
				return schemaJson; // No tasks to process
			}

			JsonElement[] processedTasks = [.. tasksElement
				.EnumerateArray()
				.Select(
					(item, index) =>
						!item.TryGetProperty(propertyName: "id", value: out _)
							? PlannerJsonProcessor.ProcessManualEntry(item: item, timestamp: timestamp, sequence: index + 1)
							: PlannerJsonProcessor.ProcessExistingEntry(item: item, timestamp: timestamp, sequence: index + 1)
				),];

			// Rebuild the v1 schema object with processed tasks
			Dictionary<string, JsonElement> v1Schema = new()
			{
				{ "version", versionProp },
				{ "tasks", JsonSerializer.SerializeToElement(processedTasks) },
			};

			// Preserve coverAndClosingComments if present
			if (schema.TryGetProperty(propertyName: "coverAndClosingComments", value: out JsonElement coverAndClosingComments))
			{
				v1Schema["coverAndClosingComments"] = coverAndClosingComments;
			}

			return JsonSerializer.Serialize(v1Schema);
		}

		// V0 format (raw array)
		if (schema.ValueKind != JsonValueKind.Array)
		{
			return schemaJson;
		}

		JsonElement[] items = [.. schema
			.EnumerateArray()
			.Select(
				(item, index) =>
					!item.TryGetProperty(propertyName: "id", value: out _)
						? PlannerJsonProcessor.ProcessManualEntry(item: item, timestamp: timestamp, sequence: index + 1)
						: PlannerJsonProcessor.ProcessExistingEntry(item: item, timestamp: timestamp, sequence: index + 1)
			),];

		return JsonSerializer.Serialize(items);
	}

	private static JsonElement ProcessExistingEntry(JsonElement item, string timestamp, int sequence)
	{
		Dictionary<string, JsonElement>? mutableItem = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
			item.GetRawText()
		);
		if (mutableItem == null)
		{
			return item;
		}

		mutableItem["id"] = JsonSerializer.SerializeToElement($"T-{timestamp}-{sequence:D3}");

		if (mutableItem.ContainsKey("hazards"))
		{
			List<Dictionary<string, JsonElement>> hazards =
				JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(mutableItem["hazards"].GetRawText())
				?? [];
			for (int i = 0; i < hazards.Count; i++)
			{
				hazards[i] = PlannerJsonProcessor.CreateOrderedHazard(hazard: hazards[i], timestamp: timestamp, hazardIndex: i);
			}
			mutableItem["hazards"] = JsonSerializer.SerializeToElement(hazards);
		}

		if (mutableItem.ContainsKey("inspectionCriteria"))
		{
			List<Dictionary<string, JsonElement>> criteria =
				JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
					mutableItem["inspectionCriteria"].GetRawText()
				) ?? [];
			for (int i = 0; i < criteria.Count; i++)
			{
				criteria[i] = PlannerJsonProcessor.CreateOrderedInspectionCriteria(criteria: criteria[i]);
			}
			mutableItem["inspectionCriteria"] = JsonSerializer.SerializeToElement(criteria);
		}

		return JsonSerializer.SerializeToElement(mutableItem);
	}

	private static JsonElement ProcessManualEntry(JsonElement item, string timestamp, int sequence)
	{
		Dictionary<string, JsonElement>? mutableItem = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
			item.GetRawText()
		);
		if (mutableItem == null)
		{
			return item;
		}

		Dictionary<string, JsonElement> orderedItem = PlannerJsonProcessor.CreateOrderedItem(mutableItem: mutableItem, timestamp: timestamp, sequence: sequence);

		if (mutableItem.ContainsKey("hazards"))
		{
			PlannerJsonProcessor.ProcessHazards(mutableItem: mutableItem, orderedItem: orderedItem, timestamp: timestamp);
		}

		if (mutableItem.ContainsKey("inspectionCriteria"))
		{
			PlannerJsonProcessor.ProcessInspectionCriteria(mutableItem: mutableItem, orderedItem: orderedItem);
		}

		return JsonSerializer.SerializeToElement(orderedItem);
	}

	private static Dictionary<string, JsonElement> CreateOrderedItem(
		Dictionary<string, JsonElement> mutableItem,
		string timestamp,
		int sequence
	)
	{
		return new Dictionary<string, JsonElement>
		{
			{ "id", JsonSerializer.SerializeToElement($"T-{timestamp}-{sequence:D3}") },
			{ "name", mutableItem.GetValueOrDefault(key: "name", defaultValue: JsonSerializer.SerializeToElement("")) },
			{ "description", mutableItem.GetValueOrDefault(key: "description", defaultValue: JsonSerializer.SerializeToElement("")) },
			{ "duration", mutableItem.GetValueOrDefault(key: "duration", defaultValue: JsonSerializer.SerializeToElement("0")) },
			{ "planType", JsonSerializer.SerializeToElement("preventive") },
			{ "priority", JsonSerializer.SerializeToElement("medium") },
			{
				"resources",
				mutableItem.GetValueOrDefault(
					key: "resources",
					defaultValue: JsonSerializer.SerializeToElement(new[] { new { name = "", }, })
				)
			},
			{
				"prerequisites",
				mutableItem.GetValueOrDefault(key: "prerequisites", defaultValue: JsonSerializer.SerializeToElement(Array.Empty<string>()))
			},
		};
	}

	private static void ProcessHazards(
		Dictionary<string, JsonElement> mutableItem,
		Dictionary<string, JsonElement> orderedItem,
		string timestamp
	)
	{
		List<Dictionary<string, JsonElement>> hazards =
			JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(mutableItem["hazards"].GetRawText())
			?? [];
		for (int i = 0; i < hazards.Count; i++)
		{
			hazards[i] = PlannerJsonProcessor.CreateOrderedHazard(hazard: hazards[i], timestamp: timestamp, hazardIndex: i);
		}
		orderedItem["hazards"] = JsonSerializer.SerializeToElement(hazards);
	}

	private static Dictionary<string, JsonElement> CreateOrderedHazard(
		Dictionary<string, JsonElement> hazard,
		string timestamp,
		int hazardIndex
	)
	{
		Dictionary<string, JsonElement> orderedHazard = new()
		{
			{ "hazardId", JsonSerializer.SerializeToElement($"H-{timestamp}-{hazardIndex + 1:D2}") },
			{ "type", hazard.GetValueOrDefault(key: "type", defaultValue: JsonSerializer.SerializeToElement("")) },
			{
				"sourceEvidence",
				hazard.GetValueOrDefault(
					key: "sourceEvidence",
					defaultValue: JsonSerializer.SerializeToElement(
						new
						{
							images = new[]
							{
								new
								{
									type = "",
									logitId = "",
									assetId = "",
									observations = "",
									comments = Array.Empty<object>(),
									sentiment = "",
									evidenceRegulatoryCodes = Array.Empty<object>(),
								},
							},
							relevance = "",
						}
					)
				)
			},
			{
				"regulatoryCodes",
				hazard.GetValueOrDefault(key: "regulatoryCodes", defaultValue: JsonSerializer.SerializeToElement(Array.Empty<object>()))
			},
		};

		if (hazard.ContainsKey("sourceEvidence"))
		{
			try
			{
				Dictionary<string, JsonElement>? sourceEvidenceDict = JsonSerializer.Deserialize<
					Dictionary<string, JsonElement>
				>(hazard["sourceEvidence"].GetRawText());
				if (sourceEvidenceDict != null && sourceEvidenceDict.ContainsKey("images"))
				{
					JsonElement images = sourceEvidenceDict["images"];
					JsonElement[] updatedImages = [.. images
						.EnumerateArray()
						.Select(img =>
						{
							Dictionary<string, JsonElement>? imgDict = JsonSerializer.Deserialize<
								Dictionary<string, JsonElement>
							>(img.GetRawText());
							if (imgDict != null && !imgDict.ContainsKey("evidenceRegulatoryCodes"))
							{
								imgDict["evidenceRegulatoryCodes"] = JsonSerializer.SerializeToElement(
									Array.Empty<object>()
								);
							}
							return JsonSerializer.SerializeToElement(imgDict);
						}),];

					sourceEvidenceDict["images"] = JsonSerializer.SerializeToElement(updatedImages);
					orderedHazard["sourceEvidence"] = JsonSerializer.SerializeToElement(sourceEvidenceDict);
				}
			}
			catch (JsonException)
			{
				// If any JSON parsing fails, keep the default structure
			}
		}

		if (hazard.ContainsKey("subTypes"))
		{
			PlannerJsonProcessor.ProcessSubTypes(hazard: hazard, orderedHazard: orderedHazard, timestamp: timestamp, hazardIndex: hazardIndex);
		}

		return orderedHazard;
	}

	private static void ProcessSubTypes(
		Dictionary<string, JsonElement> hazard,
		Dictionary<string, JsonElement> orderedHazard,
		string timestamp,
		int hazardIndex
	)
	{
		List<Dictionary<string, JsonElement>> subTypes =
			JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(hazard["subTypes"].GetRawText()) ?? [];
		List<Dictionary<string, JsonElement>> orderedSubTypes = [.. subTypes
			.Select(
				(st, j) =>
					new Dictionary<string, JsonElement>
					{
						{
							"subTypeId",
							JsonSerializer.SerializeToElement($"ST-{timestamp}-{hazardIndex + 1:D2}-{j + 1:D2}")
						},
						{ "name", st.GetValueOrDefault(key: "name", defaultValue: JsonSerializer.SerializeToElement("")) },
						{ "severity", st.GetValueOrDefault(key: "severity", defaultValue: JsonSerializer.SerializeToElement(0)) },
						{ "impact", st.GetValueOrDefault(key: "impact", defaultValue: JsonSerializer.SerializeToElement(0)) },
						{ "likelihood", st.GetValueOrDefault(key: "likelihood", defaultValue: JsonSerializer.SerializeToElement(0)) },
						{
							"consequences",
							st.GetValueOrDefault(key: "consequences", defaultValue: JsonSerializer.SerializeToElement(""))
						},
						{
							"complianceStatus",
							st.GetValueOrDefault(
								key: "complianceStatus",
								defaultValue: JsonSerializer.SerializeToElement("needs-review")
							)
						},
						{
							"requiredTraining",
							st.GetValueOrDefault(key: "requiredTraining", defaultValue: JsonSerializer.SerializeToElement(""))
						},
						{ "ppe", st.GetValueOrDefault(key: "ppe", defaultValue: JsonSerializer.SerializeToElement("")) },
						{
							"mitigationStrategies",
							st.GetValueOrDefault(key: "mitigationStrategies", defaultValue: JsonSerializer.SerializeToElement(""))
						},
						{
							"controlMeasures",
							st.GetValueOrDefault(key: "controlMeasures", defaultValue: JsonSerializer.SerializeToElement(""))
						},
					}
			),];

		orderedHazard["subTypes"] = JsonSerializer.SerializeToElement(orderedSubTypes);
	}

	private static void ProcessInspectionCriteria(
		Dictionary<string, JsonElement> mutableItem,
		Dictionary<string, JsonElement> orderedItem
	)
	{
		List<Dictionary<string, JsonElement>> criteria =
			JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
				mutableItem["inspectionCriteria"].GetRawText()
			) ?? [];
		for (int i = 0; i < criteria.Count; i++)
		{
			criteria[i] = PlannerJsonProcessor.CreateOrderedInspectionCriteria(criteria: criteria[i]);
		}
		orderedItem["inspectionCriteria"] = JsonSerializer.SerializeToElement(criteria);
	}

	private static Dictionary<string, JsonElement> CreateOrderedInspectionCriteria(
		Dictionary<string, JsonElement> criteria
	)
	{
		Dictionary<string, JsonElement> orderedCriteria = new()
		{
			{ "name", criteria.GetValueOrDefault(key: "name", defaultValue: JsonSerializer.SerializeToElement("")) },
			{ "priority", criteria.GetValueOrDefault(key: "priority", defaultValue: JsonSerializer.SerializeToElement("medium")) },
			{
				"regulatoryCodes",
				criteria.GetValueOrDefault(key: "regulatoryCodes", defaultValue: JsonSerializer.SerializeToElement(Array.Empty<object>()))
			},
			{
				"acceptanceCriteria",
				criteria.GetValueOrDefault(key: "acceptanceCriteria", defaultValue: JsonSerializer.SerializeToElement(""))
			},
			{ "testMethod", criteria.GetValueOrDefault(key: "testMethod", defaultValue: JsonSerializer.SerializeToElement("")) },
			{
				"toleranceValues",
				criteria.GetValueOrDefault(key: "toleranceValues", defaultValue: JsonSerializer.SerializeToElement(""))
			},
			{
				"documentation",
				criteria.GetValueOrDefault(key: "documentation", defaultValue: JsonSerializer.SerializeToElement(""))
			},
			{
				"correctiveAction",
				criteria.GetValueOrDefault(key: "correctiveAction", defaultValue: JsonSerializer.SerializeToElement(""))
			},
		};

		return orderedCriteria;
	}
}
