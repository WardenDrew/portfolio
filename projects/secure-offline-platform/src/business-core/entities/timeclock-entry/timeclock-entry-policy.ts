import {
  allow,
  deny,
  readStringMetadata,
  requireActorDiffersFromMetadata,
  requireActorMatchesMetadata,
  type ProjectCapability,
  type ProjectEntityPolicy,
  type ProjectEntityPolicyContext,
  type ProjectEntityPolicyDecision
} from '@platform/data-core/offline';
import { timeclockEntryCapabilities } from './capabilities.js';

/** Workflow states that close a timeclock entry to self-service edits. */
const timeclockTerminalWorkflowStates = new Set(['approved', 'rejected']);

/** Workflow states accepted by the timeclock entry policy. */
const timeclockWorkflowStates = new Set(['draft', 'submitted', 'approved', 'rejected']);

/** Entity policy for encrypted timeclock entry events. */
export const timeclockEntryPolicy: ProjectEntityPolicy = {
  entityType: 'timeclock_entry',
  displayName: 'Timeclock Entry',
  policyVersion: 1,
  requiredMetadata: ['domainAction', 'policyVersion', 'subjectUserId', 'workflowState'],
  immutableMetadata: ['subjectUserId'],
  actions: [
    timeclockAction('timeclock_entry.create', 'Create own timeclock entry', ['created'], [
      timeclockEntryCapabilities.createSelf
    ]),
    timeclockAction('timeclock_entry.update', 'Update own timeclock entry', ['updated', 'corrected', 'merged'], [
      timeclockEntryCapabilities.updateSelf
    ]),
    timeclockAction('timeclock_entry.submit', 'Submit own timeclock entry', ['updated'], [
      timeclockEntryCapabilities.submitSelf
    ]),
    timeclockAction('timeclock_entry.approve', 'Approve timeclock entry', ['updated', 'corrected'], [
      timeclockEntryCapabilities.approveAny
    ]),
    timeclockAction('timeclock_entry.reject', 'Reject timeclock entry', ['updated', 'corrected'], [
      timeclockEntryCapabilities.rejectAny
    ]),
    timeclockAction('timeclock_entry.delete', 'Delete own timeclock entry', ['deleted'], [
      timeclockEntryCapabilities.updateSelf
    ])
  ],
  evaluate(context, action) {
    const workflowDecision = evaluateTimeclockWorkflow(context, action.domainAction);
    if (!workflowDecision.allowed) {
      return workflowDecision;
    }

    if (
      action.domainAction === 'timeclock_entry.create' ||
      action.domainAction === 'timeclock_entry.update' ||
      action.domainAction === 'timeclock_entry.submit' ||
      action.domainAction === 'timeclock_entry.delete'
    ) {
      return requireActorMatchesMetadata(
        context,
        'subjectUserId',
        `${action.label} is limited to the user's own timeclock entry`
      );
    }

    const actorDecision = requireActorDiffersFromMetadata(
      context,
      'subjectUserId',
      'Users cannot approve or reject their own timeclock entries'
    );
    if (!actorDecision.allowed) {
      return actorDecision;
    }

    return requireWorkflowState(
      context,
      action.domainAction === 'timeclock_entry.approve' ? 'approved' : 'rejected'
    );
  },
  canRead(context) {
    const subjectUserId = readStringMetadata(context.event.metadata, 'subjectUserId');
    if (subjectUserId === context.actorUserId || context.capabilities.includes(timeclockEntryCapabilities.readAny)) {
      return allow();
    }

    return deny('object_access_denied', 'This timeclock entry is limited to its subject user or a timeclock manager', {
      subjectUserId: subjectUserId ?? ''
    });
  }
};

/** Creates an action rule for the timeclock entry policy. */
function timeclockAction(
  domainAction: string,
  label: string,
  allowedEventTypes: readonly string[],
  requiredAnyCapabilities: readonly ProjectCapability[]
) {
  return { domainAction, label, allowedEventTypes, requiredAnyCapabilities };
}

/** Requires that the submitted workflow state equals an expected value. */
function requireWorkflowState(
  context: ProjectEntityPolicyContext,
  expectedWorkflowState: string
): ProjectEntityPolicyDecision {
  const workflowState = readStringMetadata(context.event.metadata, 'workflowState');
  if (workflowState === expectedWorkflowState) {
    return allow();
  }

  return deny('workflow_state_denied', `workflowState must be ${expectedWorkflowState}`, {
    expectedWorkflowState,
    workflowState: workflowState ?? ''
  });
}

/** Evaluates action-specific timeclock workflow transition rules. */
function evaluateTimeclockWorkflow(
  context: ProjectEntityPolicyContext,
  domainAction: string
): ProjectEntityPolicyDecision {
  const workflowState = readStringMetadata(context.event.metadata, 'workflowState');
  if (!workflowState || !timeclockWorkflowStates.has(workflowState)) {
    return deny('workflow_state_denied', 'workflowState is not valid for a timeclock entry', {
      workflowState: workflowState ?? ''
    });
  }

  if (domainAction === 'timeclock_entry.create') {
    if (workflowState !== 'draft') {
      return deny('workflow_state_denied', 'New timeclock entries must start in draft workflowState', {
        expectedWorkflowState: 'draft',
        workflowState
      });
    }
    return allow();
  }

  if (domainAction === 'timeclock_entry.update' || domainAction === 'timeclock_entry.delete') {
    return evaluateSelfTimeclockWrite(context, domainAction, workflowState);
  }

  if (domainAction === 'timeclock_entry.submit') {
    return evaluateTimeclockTransition(context, workflowState, 'submitted', ['draft']);
  }

  if (domainAction === 'timeclock_entry.approve') {
    return evaluateTimeclockTransition(context, workflowState, 'approved', ['submitted']);
  }

  if (domainAction === 'timeclock_entry.reject') {
    return evaluateTimeclockTransition(context, workflowState, 'rejected', ['submitted']);
  }

  return allow();
}

/** Evaluates self-service timeclock updates and deletes. */
function evaluateSelfTimeclockWrite(
  context: ProjectEntityPolicyContext,
  domainAction: string,
  workflowState: string
): ProjectEntityPolicyDecision {
  if (timeclockTerminalWorkflowStates.has(workflowState)) {
    return deny('workflow_state_denied', 'Self-service timeclock writes cannot set terminal workflow states', {
      workflowState
    });
  }

  const terminalHead = context.currentHeads.find((head) =>
    timeclockTerminalWorkflowStates.has(readStringMetadata(head.metadata, 'workflowState') ?? '')
  );
  if (terminalHead) {
    return deny('workflow_state_denied', 'Terminal timeclock entries cannot be edited by self-service actions', {
      currentHeadEventId: terminalHead.id,
      currentWorkflowState: readStringMetadata(terminalHead.metadata, 'workflowState') ?? ''
    });
  }

  const deletedHeadDecision = requireNoDeletedCurrentHeads(
    context,
    'Self-service timeclock writes cannot supersede deleted current heads'
  );
  if (!deletedHeadDecision.allowed) {
    return deletedHeadDecision;
  }

  if (
    domainAction === 'timeclock_entry.delete' ||
    (domainAction === 'timeclock_entry.update' && workflowState === 'draft')
  ) {
    const draftHeadDecision = requireOnlyNonDeletedWorkflowHeads(
      context,
      ['draft'],
      'Self-service timeclock draft/delete actions require current draft heads',
      'Self-service timeclock draft/delete actions require every current head to be a non-deleted draft'
    );
    if (!draftHeadDecision.allowed) {
      return draftHeadDecision;
    }
  }

  if (domainAction === 'timeclock_entry.update' && workflowState === 'submitted') {
    const submittedHeadDecision = requireOnlyNonDeletedWorkflowHeads(
      context,
      ['submitted'],
      'Self-service submitted timeclock edits require current submitted heads',
      'Self-service submitted timeclock edits require every current head to be non-deleted and submitted'
    );
    if (!submittedHeadDecision.allowed) {
      return submittedHeadDecision;
    }
  }

  return allow();
}

/** Requires that no current head is deleted before a self-service successor. */
function requireNoDeletedCurrentHeads(
  context: ProjectEntityPolicyContext,
  message: string
): ProjectEntityPolicyDecision {
  const deletedHead = context.currentHeads.find((head) => head.deletedAt !== null);
  if (!deletedHead) {
    return allow();
  }

  return deny('workflow_state_denied', message, {
    currentHeadDeletedAt: deletedHead.deletedAt ?? '',
    currentHeadEventId: deletedHead.id,
    currentWorkflowState: readStringMetadata(deletedHead.metadata, 'workflowState') ?? ''
  });
}

/** Requires every current head to be non-deleted and in one of the allowed workflow states. */
function requireOnlyNonDeletedWorkflowHeads(
  context: ProjectEntityPolicyContext,
  allowedCurrentWorkflowStates: readonly string[],
  missingHeadsMessage: string,
  invalidHeadMessage: string
): ProjectEntityPolicyDecision {
  if (context.currentHeads.length === 0) {
    return deny('workflow_state_denied', missingHeadsMessage, {
      currentHeadCount: 0,
      expectedCurrentWorkflowStates: [...allowedCurrentWorkflowStates]
    });
  }

  const invalidHead = context.currentHeads.find((head) => {
    const currentWorkflowState = readStringMetadata(head.metadata, 'workflowState');
    return (
      head.deletedAt !== null ||
      !currentWorkflowState ||
      !allowedCurrentWorkflowStates.includes(currentWorkflowState)
    );
  });
  if (invalidHead) {
    return deny('workflow_state_denied', invalidHeadMessage, {
      currentHeadDeletedAt: invalidHead.deletedAt ?? '',
      currentHeadEventId: invalidHead.id,
      currentWorkflowState: readStringMetadata(invalidHead.metadata, 'workflowState') ?? '',
      expectedCurrentWorkflowStates: [...allowedCurrentWorkflowStates]
    });
  }

  return allow();
}

/** Evaluates manager-controlled timeclock workflow transitions. */
function evaluateTimeclockTransition(
  context: ProjectEntityPolicyContext,
  workflowState: string,
  expectedWorkflowState: string,
  allowedCurrentWorkflowStates: readonly string[]
): ProjectEntityPolicyDecision {
  if (workflowState !== expectedWorkflowState) {
    return deny('workflow_state_denied', `workflowState must be ${expectedWorkflowState}`, {
      expectedWorkflowState,
      workflowState
    });
  }

  if (context.currentHeads.length === 0) {
    return deny('workflow_state_denied', 'Timeclock workflow transitions require current server heads', {
      expectedCurrentWorkflowStates: [...allowedCurrentWorkflowStates]
    });
  }

  return requireOnlyNonDeletedWorkflowHeads(
    context,
    allowedCurrentWorkflowStates,
    'Timeclock workflow transitions require current server heads',
    'Timeclock workflow transition requires every current head to be non-deleted and in an allowed state'
  );
}
