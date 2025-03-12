import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DropboxAuthCallbackComponent } from './dropbox-auth-callback.component';

describe('DropboxAuthCallbackComponent', () => {
  let component: DropboxAuthCallbackComponent;
  let fixture: ComponentFixture<DropboxAuthCallbackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DropboxAuthCallbackComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DropboxAuthCallbackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
