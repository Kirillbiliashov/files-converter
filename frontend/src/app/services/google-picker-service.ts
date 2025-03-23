import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

declare var gapi: any;
declare var google: any;

@Injectable({
  providedIn: 'root',
})
export class GooglePickerService {
  private appId = '1008106191398-bbdjcb8o46kntkjochbbpcrj64o457lo.apps.googleusercontent.com'; 
  private fileSelectedSubject = new Subject<any>(); // Subject to emit file selection events

  // Observable that components can subscribe to for file selection events
  fileSelected$: Observable<any> = this.fileSelectedSubject.asObservable();

  constructor() {
    this.loadPicker(); // Load picker when the service initializes
  }

  // ✅ Load Google Picker API (No authentication needed)
  async loadPicker(): Promise<void> {
    return new Promise((resolve) => {
      gapi.load('picker', resolve);
    });
  }

  // ✅ Create Picker using the provided access token
  createPicker(accessToken: string): void {
    if (!google || !google.picker) {
      console.error('Google Picker API is not loaded yet.');
      return;
    }

    const picker = new google.picker.PickerBuilder()
      .addView(google.picker.ViewId.DOCS) // Drive File Selector
      .setOAuthToken(accessToken) // Use the access token received from backend
      .setAppId(this.appId)
      .setCallback(this.pickerCallback.bind(this))
      .build();

    picker.setVisible(true);
  }

  // Handle File Selection
  private pickerCallback(data: any): void {
    if (data.action === google.picker.Action.PICKED) {
      const file = data.docs[0];
      this.fileSelectedSubject.next(file);
    }
  }
}
