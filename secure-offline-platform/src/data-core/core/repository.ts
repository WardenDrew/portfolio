import { RecordConflictError, RecordNotFoundError } from './errors.js';
import {
  Clock,
  EntityType,
  JsonObject,
  OperationContext,
  PersistedRecord,
  VersionedRecord,
  cloneJson,
  systemClock,
  toIsoString
} from './types.js';
import { EntityDefinition, migrateRecord, validateRecord } from './migrations.js';
import { RecordListQuery, RecordStore } from './storage.js';

/** Options controlling repository time and read-time migration behavior. */
export interface RepositoryOptions {
  /** Clock used for record timestamps. */
  readonly clock?: Clock;
  /** Whether migrated records should be persisted after reads. */
  readonly persistMigrationsOnRead?: boolean;
}

/** Options required when creating a record. */
export interface CreateRecordOptions {
  /** Record id to persist. */
  readonly id: string;
  /** Operation context passed to validation hooks. */
  readonly context?: OperationContext;
}

/** Options accepted when saving an existing record. */
export interface SaveRecordOptions {
  /** Expected optimistic concurrency version. */
  readonly expectedRecordVersion?: number;
  /** Operation context passed to validation hooks. */
  readonly context?: OperationContext;
}

/** Repository that applies entity validation, migration, and optimistic concurrency. */
export class VersionedRepository<
  TEntityType extends EntityType,
  TData extends JsonObject
> {
  /** Entity definition enforced by this repository. */
  readonly definition: EntityDefinition<TEntityType, TData>;
  /** Record store that persists raw records. */
  private readonly store: RecordStore;
  /** Clock used to generate timestamps. */
  private readonly clock: Clock;
  /** Whether read-time migrations are written back to storage. */
  private readonly persistMigrationsOnRead: boolean;

  /** Creates a repository for one entity definition. */
  constructor(
    definition: EntityDefinition<TEntityType, TData>,
    store: RecordStore,
    options: RepositoryOptions = {}
  ) {
    this.definition = definition;
    this.store = store;
    this.clock = options.clock ?? systemClock;
    this.persistMigrationsOnRead = options.persistMigrationsOnRead ?? true;
  }

  /** Creates a new record with version 1 and current schema version. */
  async create(
    data: TData,
    options: CreateRecordOptions
  ): Promise<VersionedRecord<TEntityType, TData>> {
    const now = toIsoString(this.clock.now());
    const record: VersionedRecord<TEntityType, TData> = {
      id: options.id,
      entityType: this.definition.type,
      schemaVersion: this.definition.currentSchemaVersion,
      recordVersion: 1,
      data: cloneJson(data),
      createdAt: now,
      updatedAt: now,
      deletedAt: null
    };

    validateRecord(this.definition, record, options.context);
    return (await this.store.put(record, {
      requireAbsent: true
    })) as VersionedRecord<TEntityType, TData>;
  }

  /** Gets a record by id, migrating it before returning when needed. */
  async get(id: string, context: OperationContext = {}): Promise<VersionedRecord<TEntityType, TData> | null> {
    const rawRecord = await this.store.get(this.definition.type, id);

    if (!rawRecord) {
      return null;
    }

    return this.migrateAfterRead(rawRecord, context);
  }

  /** Gets a record by id or throws when it does not exist. */
  async getRequired(id: string, context: OperationContext = {}): Promise<VersionedRecord<TEntityType, TData>> {
    const record = await this.get(id, context);

    if (!record) {
      throw new RecordNotFoundError(this.definition.type, id);
    }

    return record;
  }

  /** Lists records for this entity type, migrating each result before returning. */
  async list(
    query: Omit<RecordListQuery, 'entityType'> = {},
    context: OperationContext = {}
  ): Promise<VersionedRecord<TEntityType, TData>[]> {
    const rawRecords = await this.store.list({
      ...query,
      entityType: this.definition.type
    });
    const migrated: VersionedRecord<TEntityType, TData>[] = [];

    for (const rawRecord of rawRecords) {
      migrated.push(await this.migrateAfterRead(rawRecord, context));
    }

    return migrated;
  }

  /** Saves an existing record with optimistic concurrency validation. */
  async save(
    record: VersionedRecord<TEntityType, TData>,
    options: SaveRecordOptions = {}
  ): Promise<VersionedRecord<TEntityType, TData>> {
    const expectedRecordVersion = options.expectedRecordVersion ?? record.recordVersion;
    const nextRecord: VersionedRecord<TEntityType, TData> = {
      ...record,
      data: cloneJson(record.data),
      schemaVersion: this.definition.currentSchemaVersion,
      recordVersion: record.recordVersion + 1,
      updatedAt: toIsoString(this.clock.now())
    };

    validateRecord(this.definition, nextRecord, options.context);

    return (await this.store.put(nextRecord, {
      expectedRecordVersion
    })) as VersionedRecord<TEntityType, TData>;
  }

  /** Updates one record by transforming its data payload. */
  async update(
    id: string,
    update: (data: TData, record: VersionedRecord<TEntityType, TData>) => TData,
    options: SaveRecordOptions = {}
  ): Promise<VersionedRecord<TEntityType, TData>> {
    const current = await this.getRequired(id, options.context);
    const nextData = update(cloneJson(current.data), current);

    return this.save(
      {
        ...current,
        data: nextData
      },
      options
    );
  }

  /** Soft-deletes a record by setting deletedAt. */
  async softDelete(
    id: string,
    options: SaveRecordOptions = {}
  ): Promise<VersionedRecord<TEntityType, TData>> {
    const current = await this.getRequired(id, options.context);
    return this.save(
      {
        ...current,
        deletedAt: toIsoString(this.clock.now())
      },
      options
    );
  }

  /** Hard-deletes a record from the backing store. */
  async hardDelete(id: string, options: SaveRecordOptions = {}): Promise<void> {
    const current = await this.getRequired(id, options.context);
    await this.store.delete(this.definition.type, id, {
      expectedRecordVersion: options.expectedRecordVersion ?? current.recordVersion
    });
  }

  /** Migrates a raw record and optionally writes the migrated form back to storage. */
  private async migrateAfterRead(
    rawRecord: PersistedRecord,
    context: OperationContext
  ): Promise<VersionedRecord<TEntityType, TData>> {
    const migrationResult = migrateRecord(this.definition, rawRecord, {
      clock: this.clock,
      context
    });

    if (!migrationResult.migrated || !this.persistMigrationsOnRead) {
      return migrationResult.record;
    }

    const migratedRecord: VersionedRecord<TEntityType, TData> = {
      ...migrationResult.record,
      recordVersion: rawRecord.recordVersion + 1,
      updatedAt: toIsoString(this.clock.now())
    };

    try {
      return (await this.store.put(migratedRecord, {
        expectedRecordVersion: rawRecord.recordVersion
      })) as VersionedRecord<TEntityType, TData>;
    } catch (error) {
      if (error instanceof RecordConflictError) {
        return migrationResult.record;
      }

      throw error;
    }
  }
}
