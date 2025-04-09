import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { UserInfo } from "../../models/user-info";

@Injectable({
    providedIn: 'root'
})
export class UserService {

    constructor(private http: HttpClient) { }

    getUserInfo() {
        return this.http.get<UserInfo>(`${environment.apiBaseUrl}/user`);
    }

    deleteUserAccount() {
        return this.http.post(`${environment.apiBaseUrl}/user/delete`, {});
    }

    updatePreferences(deleteFilesAutomatically: boolean) {
        const updatedPreferencesBody = {
            deleteFilesAutomatically: deleteFilesAutomatically
        };
        return this.http.post(`${environment.apiBaseUrl}/user/update-preferences`, updatedPreferencesBody)
    }

}