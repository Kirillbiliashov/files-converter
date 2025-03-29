import { CurrentUser } from "./current-user";

export class AuthResult {
    constructor(public token: string, public user: CurrentUser, public tokenExpirationDate: Date) {}
}