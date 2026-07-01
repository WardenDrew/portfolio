using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Platform.Common.Permissions;

/// <summary>
/// 
/// </summary>
public interface IScopeBuilder
{
	/// <summary>
	/// The parent scope of this scope. The parent's key is prepended before this key during string operations
	/// </summary>
	/// <param name="parent"></param>
	/// <returns></returns>
	public IScopeBuilder Parent(Scope parent);
	
	/// <summary>
	/// Mark the Scope as privileged. It will not be issued by default unless explicitly elevated to.
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder Privileged(bool value = true);
	
	/// <summary>
	/// Mark the Scope as an Organization scope.
	/// Without Organization information in the access token this scope is meaningless
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder Organization(bool value = true);
	
	/// <summary>
	/// Mark the Scope as a Group scope.
	/// Without Group information in the access token this scope is meaningless
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder Group(bool value = true);
	
	/// <summary>
	/// Mark the Scope as an Assignable scope.
	/// If this scope is available for Organizations to select when building Roles
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder Assignable(bool value = true);

	/// <summary>
	/// Adds the specified suffix to thelist of allowed suffixes
	/// </summary>
	/// <param name="suffix"></param>
	/// <returns></returns>
	public IScopeBuilder AddSuffix(string suffix);

	/// <summary>
	/// Adds the "ro" suffix to the list of allowed suffixes
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder AddReadOnlySuffix();

	/// <summary>
	/// Clear all suffixes on this scope (for cases of inheritance)
	/// </summary>
	/// <returns></returns>
	public IScopeBuilder ClearSuffixes();
	
	/// <summary>
	/// Build the Scope object
	/// </summary>
	/// <returns></returns>
	public Scope Build();
}

/// <summary>
/// OpenID Scopes used for Identity Tokens
/// </summary>
public partial class Scope : IScopeBuilder
{
	/// <summary>
	/// The Read-Only scope suffix
	/// </summary>
	public const string READ_ONLY_SUFFIX = "ro";
	
	private const string KEY_PATTERN = @"^[0-9a-z_]+$";
	[GeneratedRegex(Scope.KEY_PATTERN)]
	private static partial Regex KeyRegex();
	
	/// <summary>
	/// The Key of the scope, used in the "scope" claim. Must be [0-9a-z_:] character set
	/// </summary>
	public required string Key { get; init; }

	/// <summary>
	/// The parent scope of this scope
	/// </summary>
	public Scope? ParentScope { get; private set; }

	/// <summary>
	/// If this scope is privileged. If so it will require re-authentication to receive.
	/// </summary>
	public bool IsPrivileged { get; private set; }
	
	/// <summary>
	/// If this scope is an Organization specific scope.
	/// Without organization information in the token this scope is meaningless.
	/// </summary>
	public bool IsOrganization { get; private set; }
	
	/// <summary>
	/// If this scope is an Group specific scope.
	/// Without group information in the token this scope is meaningless.
	/// </summary>
	public bool IsGroup { get; private set; }

	/// <summary>
	/// If this scope is available for Organizations to select when building Roles
	/// </summary>
	public bool IsAssignable { get; private set; }

	/// <summary>
	/// Allowed Suffixes to the scope, typically "ro" for Readonly but other suffixes may be added
	/// </summary>
	public HashSet<string> AllowedSuffixes { get; private set; } = [];

	/// <summary>
	/// Constructor private to ensure use of Build() method so scopes are added to dictionary
	/// </summary>
	private Scope() { }

	/// <inheritdoc />
	public override string ToString()
	{
		return this.ParentScope is not null
			? $"{this.ParentScope}.{this.Key}"
			: this.Key;
	}

	/// <summary>
	/// Indexor to return this scope with the given suffix appened
	/// </summary>
	/// <param name="suffix"></param>
	public string this[string suffix]
	{
		get
		{
			if (!AllowedSuffixes.Contains(suffix))
			{
				throw new InvalidOperationException(
					$"The suffix \'{suffix}\' is not allowed for the given scope \'{this}\'.");
			}
			
			return $"{this}:{suffix}";
		}
	}

	/// <summary>
	/// The ReadOnly suffix of this Scope
	/// </summary>
	/// <returns></returns>
	// ReSharper disable once InconsistentNaming
	public string RO => this[Scope.READ_ONLY_SUFFIX];
	
	/// <summary>
	/// Implicit cast
	/// </summary>
	/// <param name="scope"></param>
	/// <returns></returns>
	public static implicit operator string(Scope scope)
	{
		return scope.ToString();
	}

	/// <summary>
	/// All scopes dictionary used for lookups
	/// </summary>
	private static readonly Dictionary<string, Scope> allScopes = new();
	
	/// <summary>
	/// Try to get a scope by its key
	/// </summary>
	/// <param name="key"></param>
	/// <param name="scope"></param>
	/// <returns></returns>
	public static bool TryGetValue(string key, out Scope? scope)
	{
		return Scope.allScopes.TryGetValue(key: key, value: out scope);
	}

	/// <summary>
	/// Create a new Scope Builder
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static IScopeBuilder Create(string key)
	{
		if (!Scope.KeyRegex().IsMatch(key))
		{
			throw new InvalidOperationException(
				$"The scope \'{key}\' uses dis-allowed characters. " +
				"Only scopes with lowercase alphanumerics and the underscore symbol are allowed.");
		}

		IScopeBuilder scope = new Scope()
		{
			Key = key,
		};

		return scope;
	}

	/// <summary>
	/// Create a child scope of this parent scope. The Child scope inherits properties of the parent scope by default
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public IScopeBuilder Child(string key)
	{
		return Scope.Create(key)
			.Parent(this);
	}

	/// <inheritdoc />
	public IScopeBuilder Parent(Scope parent)
	{
		this.ParentScope = parent;
		this.IsPrivileged = parent.IsPrivileged;
		this.IsOrganization = parent.IsOrganization;
		this.IsGroup = parent.IsGroup;
		this.IsAssignable = parent.IsAssignable;
		foreach (string suffix in parent.AllowedSuffixes)
		{
			this.AllowedSuffixes.Add(suffix);
		}
		return this;
	}

	/// <inheritdoc />
	public IScopeBuilder Privileged(bool value = true)
	{
		this.IsPrivileged = value;
		return this;
	}

	/// <inheritdoc />
	public IScopeBuilder Organization(bool value = true)
	{
		this.IsOrganization = value;
		return this;
	}
	
	/// <inheritdoc />
	public IScopeBuilder Group(bool value = true)
	{
		this.IsGroup = value;
		return this;
	}
	
	/// <inheritdoc />
	public IScopeBuilder Assignable(bool value = true)
	{
		this.IsAssignable = value;
		return this;
	}

	/// <inheritdoc />
	public IScopeBuilder AddSuffix(string suffix)
	{
		if (!Scope.KeyRegex().IsMatch(suffix))
		{
			throw new InvalidOperationException(
				$"The suffix \'{suffix}\' uses dis-allowed characters. " +
				"Only suffixes with lowercase alphanumerics and the underscore symbol are allowed.");
		}
		
		// HashSet prevents duplicates
		this.AllowedSuffixes.Add(suffix);
		return this;
	}
	
	/// <inheritdoc />
	public IScopeBuilder AddReadOnlySuffix()
	{
		return this.AddSuffix(Scope.READ_ONLY_SUFFIX);
	}
	
	/// <inheritdoc />
	public IScopeBuilder ClearSuffixes()
	{
		this.AllowedSuffixes = [];
		return this;
	}

	/// <inheritdoc />
	public Scope Build()
	{
		Scope.allScopes.Add(key: this, value: this);
		return this;
	}

	/// <summary>
	/// Directly create a scope key with default settings
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static Scope Build(string key)
	{
		return Scope.Create(key).Build();
	}
}