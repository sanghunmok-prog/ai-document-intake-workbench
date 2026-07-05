import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { IntakeApiService, IntakeDocument, ReviewDetail, ReviewQueueItem, SampleDocument } from './intake-api.service';

type AppView = 'overview' | 'intake' | 'reviewQueue' | 'reviewDetail';

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
  protected readonly createdDocument = signal<IntakeDocument | null>(null);
  protected readonly samplesLoading = signal(false);
  protected readonly intakeLoading = signal(false);
  protected readonly reviewQueueLoading = signal(false);
  protected readonly reviewDetailLoading = signal(false);
  protected readonly creatingSampleId = signal<string | null>(null);
  protected readonly sampleError = signal<string | null>(null);
  protected readonly intakeError = signal<string | null>(null);
  protected readonly reviewQueueError = signal<string | null>(null);
  protected readonly reviewDetailError = signal<string | null>(null);
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
    return error instanceof Error ? error.message : fallback;
  }
}
