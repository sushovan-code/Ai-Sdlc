/** The wrap-up views of a project: traceability and the audit trail. */

export interface TraceLink {
  id: string;
  title: string;
}

export interface TraceRow {
  requirementId: string;
  title: string;
  architecture: TraceLink[];
  tasks: TraceLink[];
  testCases: TraceLink[];
}

export interface Traceability {
  rows: TraceRow[];
}

export interface AuditEntry {
  occurredAtUtc: string;
  actorEmail: string | null;
  action: string;
  outcome: 'Success' | 'Failure';
  details: string | null;
}

export interface AuditPage {
  items: AuditEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
}
