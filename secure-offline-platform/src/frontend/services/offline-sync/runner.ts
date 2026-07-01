import {
  claimSyncQueueRecordForRun,
  getSyncQueueRecord,
  listReadySyncQueueRecords,
  type SyncQueueRecord,
} from 'src/services/local-database';

import { touchSyncMetadata } from './metadata';
import { pullProjectUpdateRecord } from './pull';
import { pushEncryptedEventRecord } from './push';
import { queueProjectPull } from './queue';
import { applyRemoteProjectAccessPurgeDirectives } from './purge-directives';
import { readErrorMessage } from './retry-policy';
import { countSyncRunResult, createOfflineSyncRunSummary } from './status-summaries';
import { updateSyncRecord } from './sync-record-status';
import { projectEventOfflineSyncTransport } from './transport';
import type {
  OfflineSyncRunOptions,
  OfflineSyncRunSummary,
  OfflineSyncTransport,
  QueueProjectPullInput,
  SyncRunResult,
} from './types';

export async function runOfflineSync(options: OfflineSyncRunOptions = {}): Promise<OfflineSyncRunSummary> {
  const now = new Date().toISOString();
  const transport = options.transport ?? projectEventOfflineSyncTransport;
  await applyRemoteProjectAccessPurgeDirectives(transport, 'sync');

  const queued = await listReadySyncQueueRecords(now, options.maxItems ?? 20);
  const summary = createOfflineSyncRunSummary(queued.length);

  for (const record of queued) {
    const result = await runSyncRecord(record, transport);
    if (result) {
      countSyncRunResult(summary, result);
    }
  }

  await touchSyncMetadata(new Date().toISOString());
  return summary;
}

export async function runImmediateSyncQueueRecord(
  record: SyncQueueRecord,
  options: OfflineSyncRunOptions = {},
): Promise<OfflineSyncRunSummary> {
  const transport = options.transport ?? projectEventOfflineSyncTransport;
  await applyRemoteProjectAccessPurgeDirectives(transport, 'sync');

  const summary = createOfflineSyncRunSummary(1);
  const result = await runSyncRecord(record, transport);
  if (result) {
    countSyncRunResult(summary, result);
  }
  await touchSyncMetadata(new Date().toISOString());
  return summary;
}

export async function runImmediateProjectPull(
  input: QueueProjectPullInput,
  options: OfflineSyncRunOptions = {},
): Promise<OfflineSyncRunSummary> {
  const record = await queueProjectPull(input);
  return runImmediateSyncQueueRecord(record, options);
}

async function runSyncRecord(
  record: SyncQueueRecord,
  transport: OfflineSyncTransport | undefined,
): Promise<SyncRunResult | null> {
  const claimed = await claimSyncQueueRecordForRun(record.idempotencyKey, new Date().toISOString());
  if (!claimed) {
    return skippedClaimResult(record);
  }

  try {
    if (claimed.operation === 'push_encrypted_event_envelope') {
      return await pushEncryptedEventRecord(claimed, transport);
    }

    return await pullProjectUpdateRecord(claimed, transport);
  } catch (error) {
    await updateSyncRecord(claimed, 'failed', readErrorMessage(error));
    return 'failed';
  }
}

async function skippedClaimResult(record: SyncQueueRecord): Promise<SyncRunResult | null> {
  const current = await getSyncQueueRecord(record.idempotencyKey);
  const status = current?.status ?? record.status;
  if (status === 'synced') {
    return record.operation === 'push_encrypted_event_envelope' ? 'pushed' : 'pulled';
  }
  if (status === 'blocked') {
    return 'blocked';
  }
  if (status === 'authorization_blocked') {
    return 'authorizationBlocked';
  }
  if (status === 'conflicted') {
    return 'conflicted';
  }
  if (status === 'tombstone_blocked') {
    return 'tombstoneBlocked';
  }
  if (status === 'failed') {
    return 'failed';
  }
  return null;
}
