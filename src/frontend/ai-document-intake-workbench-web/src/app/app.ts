import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { IntakeApiService, IntakeDocument, SampleDocument } from './intake-api.service';

type AppView = 'overview' | 'intake';

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
  protected readonly createdDocument = signal<IntakeDocument | null>(null);
  protected readonly samplesLoading = signal(false);
  protected readonly intakeLoading = signal(false);
  protected readonly creatingSampleId = signal<string | null>(null);
  protected readonly sampleError = signal<string | null>(null);
  protected readonly intakeError = signal<string | null>(null);

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

  protected async refreshIntake(): Promise<void> {
    await Promise.all([
      this.loadSamples(true),
      this.loadIntakeDocuments()
    ]);
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

  private describeError(error: unknown, fallback: string): string {
    return error instanceof Error ? error.message : fallback;
  }
}
