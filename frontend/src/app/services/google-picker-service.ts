import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

declare var gapi: any;
declare var google: any;

@Injectable({
  providedIn: 'root',
})
export class GooglePickerService {
  private appId = '1008106191398-bbdjcb8o46kntkjochbbpcrj64o457lo.apps.googleusercontent.com'; 
  private fileSelectedSubject = new Subject<any>(); 

  fileSelected$: Observable<any> = this.fileSelectedSubject.asObservable();

  constructor() {
    this.loadPicker();
  }

  async loadPicker(): Promise<void> {
    return new Promise((resolve) => {
      gapi.load('picker', resolve);
    });
  }

  createPicker(accessToken: string): void {
    if (!google || !google.picker) {
      console.error('Google Picker API is not loaded yet.');
      return;
    }

    const picker = new google.picker.PickerBuilder()
      .addView(google.picker.ViewId.DOCS)
      .setOAuthToken(accessToken) 
      .setAppId(this.appId)
      .setCallback(this.pickerCallback.bind(this))
      .build();

    picker.setVisible(true);
  }

  private pickerCallback(data: any): void {
    if (data.action === google.picker.Action.PICKED) {
      const file = data.docs[0];
      this.fileSelectedSubject.next(file);
    }
  }
  
}
