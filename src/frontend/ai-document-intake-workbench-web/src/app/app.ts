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

  protected queueItemNeedsAttention(item: ReviewQueueItem): boolean {
    return item.validationFlagCount > 0 || item.overallConfidence < 0.75;
  }

  protected isFinalReview(detail: ReviewDetail): boolean {
    return Boolean(detail.reviewState?.decision)
      || ['Approved', 'Rejected', 'NeedsCorrection', 'Closed'].includes(detail.workflowStatus);
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
      this.reviewActionMessage.set(`Reviewer decision '${decision}' was recorded.`);
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
}
