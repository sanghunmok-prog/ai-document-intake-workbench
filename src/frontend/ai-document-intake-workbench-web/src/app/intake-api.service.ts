import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface SampleDocument {
  id: string;
  title: string;
  scenario: string;
  summary: string;
  contentPreview: string;
}

export interface IntakeDocument {
  id: string;
  displayName: string;
  status: string;
  sampleDocumentId: string | null;
  scenario: string | null;
  createdUtc: string;
  updatedUtc: string;
}

export interface ReviewQueueItem {
  intakeDocumentId: string;
  displayName: string;
  workflowStatus: string;
  documentType: string;
  overallConfidence: number;
  validationFlagCount: number;
  highestSeverity: string | null;
  sampleDocumentId: string | null;
  scenario: string | null;
  updatedUtc: string;
}

export interface ReviewDetail {
  intakeDocumentId: string;
  displayName: string;
  sampleDocumentId: string | null;
  scenario: string | null;
  summary: string | null;
  documentText: string | null;
  workflowStatus: string;
  reviewState: ReviewStateDetail | null;
  documentType: string;
  overallConfidence: number;
  rationale: string;
  suggestedRouting: string;
  extractedFields: ExtractedFieldDetail[];
  validationFlags: ValidationFlagDetail[];
  auditEvents: AuditEventDetail[];
  createdUtc: string;
  updatedUtc: string;
}

export interface ReviewStateDetail {
  requiresHumanReview: boolean;
  decision: string | null;
  decidedBy: string | null;
  decidedUtc: string | null;
  createdUtc: string;
  updatedUtc: string;
}

export interface ExtractedFieldDetail {
  name: string;
  value: string;
  confidence: number;
  reviewedValue: string | null;
  reviewedBy: string | null;
  reviewedUtc: string | null;
}

export interface ValidationFlagDetail {
  flagType: string;
  severity: string;
  fieldName: string | null;
  message: string;
  createdUtc: string;
}

export interface AuditEventDetail {
  eventType: string;
  message: string;
  createdUtc: string;
}

export interface ReviewFieldUpdatesRequest {
  fieldUpdates: ReviewFieldUpdateRequest[];
}

export interface ReviewFieldUpdateRequest {
  fieldName: string;
  reviewedValue: string;
}

export interface ReviewerDecisionRequest {
  decision: string;
}

export interface ReviewWorkflowResponse {
  intakeDocumentId: string;
  workflowStatus: string;
  decision: string | null;
  editedFieldCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class IntakeApiService {
  private readonly apiBaseUrl = 'http://localhost:5080';

  constructor(private readonly http: HttpClient) {}

  getSampleDocuments() {
    return this.http.get<SampleDocument[]>(`${this.apiBaseUrl}/api/sample-documents`);
  }

  getIntakeDocuments() {
    return this.http.get<IntakeDocument[]>(`${this.apiBaseUrl}/api/intake-documents`);
  }

  getReviewQueue() {
    return this.http.get<ReviewQueueItem[]>(`${this.apiBaseUrl}/api/review-queue`);
  }

  getReviewDetail(intakeDocumentId: string) {
    return this.http.get<ReviewDetail>(`${this.apiBaseUrl}/api/review-queue/${intakeDocumentId}`);
  }

  updateReviewFields(intakeDocumentId: string, request: ReviewFieldUpdatesRequest) {
    return this.http.put<ReviewWorkflowResponse>(
      `${this.apiBaseUrl}/api/intake-documents/${intakeDocumentId}/review/fields`,
      request);
  }

  recordReviewerDecision(intakeDocumentId: string, request: ReviewerDecisionRequest) {
    return this.http.post<ReviewWorkflowResponse>(
      `${this.apiBaseUrl}/api/intake-documents/${intakeDocumentId}/review/decision`,
      request);
  }

  createIntakeDocumentFromSample(sampleDocumentId: string) {
    return this.http.post<IntakeDocument>(`${this.apiBaseUrl}/api/intake-documents/from-sample`, {
      sampleDocumentId
    });
  }
}
