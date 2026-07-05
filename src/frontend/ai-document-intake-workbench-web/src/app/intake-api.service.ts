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

  createIntakeDocumentFromSample(sampleDocumentId: string) {
    return this.http.post<IntakeDocument>(`${this.apiBaseUrl}/api/intake-documents/from-sample`, {
      sampleDocumentId
    });
  }
}
