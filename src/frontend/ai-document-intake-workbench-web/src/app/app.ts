import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ExtractedFieldDetail, IntakeApiService, IntakeDocument, ReviewDetail, ReviewQueueItem, SampleDocument } from './intake-api.service';

type AppView = 'overview' | 'intake' | 'reviewQueue' | 'reviewDetail';
type ReviewerDecisionValue = 'Approved' | 'Rejected' | 'NeedsCorrection';

@Component({
  selector: 'app-root',
  imports: [DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly appName = 'AI Document Intake Workbench';
  protected readonly activeView = signal<AppView>('overview');
  protected readonly sampleDocuments = signal<SampleDocument[]>([]);
  protected readonly intakeDocuments = signal<IntakeDocument[]>([]);
  protected readonly reviewQueueItems = signal<ReviewQueueItem[]>([]);
  protected readonly reviewDetail = signal<ReviewDetail | null>(null);
  protected readonly selectedReviewDetailId = signal<string | null>(null);
  protected readonly reviewFieldValues = signal<Record<string, string>>({});
  protected readonly createdDocument = signal<IntakeDocument | null>(null);
  protected readonly samplesLoading = signal(false);
  protected readonly intakeLoading = signal(false);
  protected readonly reviewQueueLoading = signal(false);
  protected readonly reviewDetailLoading = signal(false);
  protected readonly reviewFieldsSaving = signal(false);
  protected readonly reviewDecisionSaving = signal(false);
  protected readonly creatingSampleId = signal<string | null>(null);
  protected readonly sampleError = signal<string | null>(null);
  protected readonly intakeError = signal<string | null>(null);
  protected readonly reviewQueueError = signal<string | null>(null);
  protected readonly reviewDetailError = signal<string | null>(null);
  protected readonly reviewActionError = signal<string | null>(null);
  protected readonly reviewActionMessage = signal<string | null>(null);
  protected readonly reviewDetailNotFound = signal(false);

  private readonly intakeApi = inject(IntakeApiService);

  ngOnInit(): void {
    void this.loadIntakeDocuments();
  }

  protected showOverview(): void {
    this.activeView.set('overview');
  }

  protected showIntake(): void {
    this.activeView.set('intake');
    void this.loadSamples();
    void this.loadIntakeDocuments();
  }

  protected showReviewQueue(): void {
    this.activeView.set('reviewQueue');
    void this.loadReviewQueue();
  }

  protected openReviewDetail(item: ReviewQueueItem): void {
    this.activeView.set('reviewDetail');
    this.selectedReviewDetailId.set(item.intakeDocumentId);
    this.reviewActionError.set(null);
    this.reviewActionMessage.set(null);
    void this.loadReviewDetail(item.intakeDocumentId);
  }

  protected backToReviewQueue(): void {
    this.showReviewQueue();
  }

  protected async refreshIntake(): Promise<void> {
    await Promise.all([
      this.loadSamples(true),
      this.loadIntakeDocuments()
    ]);
  }

  protected async refreshReviewQueue(): Promise<void> {
    await this.loadReviewQueue();
  }

  protected async refreshReviewDetail(): Promise<void> {
    const intakeDocumentId = this.selectedReviewDetailId();

    if (intakeDocumentId) {
      await this.loadReviewDetail(intakeDocumentId);
    }
  }

  protected async createIntakeDocument(sample: SampleDocument): Promise<void> {
    this.creatingSampleId.set(sample.id);
    this.intakeError.set(null);

    try {
      const created = await firstValueFrom(this.intakeApi.createIntakeDocumentFromSample(sample.id));
      this.createdDocument.set(created);
      await this.loadIntakeDocuments();
    } catch (error) {
      this.intakeError.set(this.describeError(error, 'Intake document could not be created.'));
    } finally {
      this.creatingSampleId.set(null);
    }
  }

  protected formatConfidence(confidence: number): string {
    return `${Math.round(confidence * 100)}%`;
  }

  protected formatDisplayLabel(value: string | null | undefined, fallback = 'Not recorded'): string {
    if (!value || value.trim().length === 0) {
      return fallback;
    }

    const words = value
      .trim()
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/[-_]+/g, ' ')
      .split(/\s+/)
      .filter(Boolean);

    if (words.length === 0) {
      return fallback;
    }

    return words.map(word => this.formatDisplayWord(word)).join(' ');
  }

  protected sampleDemonstrationLabel(sample: SampleDocument): string {
    const descriptor = `${sample.id} ${sample.scenario}`.toLowerCase();

    if (descriptor.includes('missing') || descriptor.includes('low-confidence')) {
      return 'Missing Required Field';
    }

    if (descriptor.includes('conflicting') || descriptor.includes('inconsistent')) {
      return 'Inconsistent Totals';
    }

    if (descriptor.includes('clean')) {
      return 'Clean Extraction';
    }

    return this.formatDisplayLabel(sample.scenario, 'Scenario');
  }

  protected sampleContextLabel(sampleDocumentId: string | null, scenario: string | null): string {
    if (scenario) {
      return this.formatDisplayLabel(scenario);
    }

    return this.formatDisplayLabel(sampleDocumentId, 'Local sample');
  }

  protected flagCountLabel(count: number): string {
    return `${count} ${count === 1 ? 'validation flag' : 'validation flags'}`;
  }

  protected attentionItemCount(): number {
    return this.reviewQueueItems().filter(item => this.queueItemNeedsAttention(item)).length;
  }

  protected queueItemNeedsAttention(item: ReviewQueueItem): boolean {
    return item.validationFlagCount > 0 || item.overallConfidence < 0.75;
  }

  protected isFinalReview(detail: ReviewDetail): boolean {
    return Boolean(detail.reviewState?.decision)
      || ['Approved', 'Rejected', 'NeedsCorrection', 'Closed'].includes(detail.workflowStatus);
  }

  protected humanReviewLabel(detail: ReviewDetail): string {
    if (detail.reviewState?.decision) {
      return this.formatDisplayLabel(detail.reviewState.decision);
    }

    if (detail.reviewState?.requiresHumanReview) {
      return 'Required';
    }

    return 'Not recorded';
  }

  protected reviewRequirementLabel(detail: ReviewDetail): string {
    if (!detail.reviewState) {
      return 'Not recorded';
    }

    return detail.reviewState.requiresHumanReview ? 'Required' : 'Not Required';
  }

  protected auditEventLabel(eventType: string): string {
    const auditEventLabels: Record<string, string> = {
      SampleDocumentSelected: 'Sample Document Selected',
      AiProcessingCompleted: 'AI Processing Completed',
      ValidationFlagsCreated: 'Validation Flags Created',
      WorkflowStatusChanged: 'Workflow Status Changed',
      ReviewerFieldEdited: 'Reviewer Field Edited',
      ReviewerDecisionRecorded: 'Reviewer Decision Recorded'
    };

    return auditEventLabels[eventType] ?? this.formatDisplayLabel(eventType);
  }

  protected fieldWasEdited(field: ExtractedFieldDetail): boolean {
    return field.reviewedValue !== null
      && field.reviewedValue.trim() !== field.value.trim();
  }

  protected updateReviewedFieldValue(fieldName: string, value: string): void {
    this.reviewFieldValues.update(values => ({
      ...values,
      [fieldName]: value
    }));
  }

  protected reviewFieldValue(field: ExtractedFieldDetail): string {
    return this.reviewFieldValues()[field.name] || field.reviewedValue || field.value;
  }

  protected async saveReviewedFields(detail: ReviewDetail): Promise<void> {
    if (this.isFinalReview(detail)) {
      this.reviewActionError.set('This review has already been finalized.');
      return;
    }

    const fieldUpdates = detail.extractedFields.map(field => ({
      fieldName: field.name,
      reviewedValue: (this.reviewFieldValues()[field.name] ?? '').trim()
    }));

    if (fieldUpdates.some(update => update.reviewedValue.length === 0)) {
      this.reviewActionError.set('Reviewed field values cannot be empty.');
      return;
    }

    const changedUpdates = fieldUpdates.filter(update => {
      const field = detail.extractedFields.find(item => item.name === update.fieldName);
      return field && update.reviewedValue !== this.currentReviewedValue(field);
    });

    if (changedUpdates.length === 0) {
      this.reviewActionError.set('No field changes are ready to save.');
      return;
    }

    this.reviewFieldsSaving.set(true);
    this.reviewActionError.set(null);
    this.reviewActionMessage.set(null);

    try {
      await firstValueFrom(this.intakeApi.updateReviewFields(detail.intakeDocumentId, {
        fieldUpdates: changedUpdates
      }));
      await this.loadReviewDetail(detail.intakeDocumentId);
      this.reviewActionMessage.set('Reviewed field updates were saved.');
    } catch (error) {
      this.reviewActionError.set(this.describeError(error, 'Reviewed field updates could not be saved.'));
    } finally {
      this.reviewFieldsSaving.set(false);
    }
  }

  protected async recordReviewerDecision(detail: ReviewDetail, decision: ReviewerDecisionValue): Promise<void> {
    if (this.isFinalReview(detail)) {
      this.reviewActionError.set('This review has already been finalized.');
      return;
    }

    if (!['Approved', 'Rejected', 'NeedsCorrection'].includes(decision)) {
      this.reviewActionError.set('Select a valid reviewer decision.');
      return;
    }

    this.reviewDecisionSaving.set(true);
    this.reviewActionError.set(null);
    this.reviewActionMessage.set(null);

    try {
      await firstValueFrom(this.intakeApi.recordReviewerDecision(detail.intakeDocumentId, {
        decision
      }));
      await this.loadReviewDetail(detail.intakeDocumentId);
      this.reviewActionMessage.set(`Reviewer decision "${this.formatDisplayLabel(decision)}" was recorded.`);
    } catch (error) {
      this.reviewActionError.set(this.describeError(error, 'Reviewer decision could not be recorded.'));
    } finally {
      this.reviewDecisionSaving.set(false);
    }
  }

  private async loadSamples(force = false): Promise<void> {
    if (this.sampleDocuments().length > 0 && !force) {
      return;
    }

    this.samplesLoading.set(true);
    this.sampleError.set(null);

    try {
      const samples = await firstValueFrom(this.intakeApi.getSampleDocuments());
      this.sampleDocuments.set(samples);
    } catch (error) {
      this.sampleError.set(this.describeError(error, 'Sample documents could not be loaded.'));
    } finally {
      this.samplesLoading.set(false);
    }
  }

  private async loadIntakeDocuments(): Promise<void> {
    this.intakeLoading.set(true);
    this.intakeError.set(null);

    try {
      const documents = await firstValueFrom(this.intakeApi.getIntakeDocuments());
      this.intakeDocuments.set(documents);
    } catch (error) {
      this.intakeError.set(this.describeError(error, 'Persisted intake documents could not be loaded.'));
    } finally {
      this.intakeLoading.set(false);
    }
  }

  private async loadReviewQueue(): Promise<void> {
    this.reviewQueueLoading.set(true);
    this.reviewQueueError.set(null);

    try {
      const queueItems = await firstValueFrom(this.intakeApi.getReviewQueue());
      this.reviewQueueItems.set(queueItems);
    } catch (error) {
      this.reviewQueueError.set(this.describeError(error, 'Review queue could not be loaded.'));
    } finally {
      this.reviewQueueLoading.set(false);
    }
  }

  private async loadReviewDetail(intakeDocumentId: string): Promise<void> {
    this.reviewDetailLoading.set(true);
    this.reviewDetailError.set(null);
    this.reviewDetailNotFound.set(false);
    this.reviewDetail.set(null);

    try {
      const detail = await firstValueFrom(this.intakeApi.getReviewDetail(intakeDocumentId));
      this.reviewDetail.set(detail);
      this.initializeReviewFieldValues(detail);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.reviewDetailNotFound.set(true);
      } else {
        this.reviewDetailError.set(this.describeError(error, 'Review detail could not be loaded.'));
      }
    } finally {
      this.reviewDetailLoading.set(false);
    }
  }

  private describeError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return error instanceof Error ? error.message : fallback;
  }

  private initializeReviewFieldValues(detail: ReviewDetail): void {
    this.reviewFieldValues.set(Object.fromEntries(
      detail.extractedFields.map(field => [field.name, this.currentReviewedValue(field)])
    ));
  }

  private currentReviewedValue(field: ExtractedFieldDetail): string {
    return (field.reviewedValue ?? field.value).trim();
  }

  private formatDisplayWord(word: string): string {
    const normalized = word.toLowerCase();
    const acronyms = new Set(['ai', 'api', 'id', 'url', 'utc']);

    if (acronyms.has(normalized)) {
      return normalized.toUpperCase();
    }

    if (word.length > 1 && word === word.toUpperCase()) {
      return word;
    }

    return `${normalized.charAt(0).toUpperCase()}${normalized.slice(1)}`;
  }
}
