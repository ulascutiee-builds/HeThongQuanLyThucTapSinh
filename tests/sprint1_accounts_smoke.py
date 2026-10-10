"""Sprint 1 + persistent accounts integration checks against a running server.

Run with SPRINT1_TEST_HR_PASSWORD and SPRINT1_TEST_ADMIN_PASSWORD set.
API_BASE (or SPRINT1_URL) may contain the origin or a trailing /api.
SPRINT1_TEST_MAIL_DIR points to the local EML pickup directory when necessary.
The suite creates uniquely tagged fixtures and preserves business history. It
temporarily changes Mentor permissions, restoring them in a finally block.
Run alongside other integration suites sequentially, against a test database.
Only Python's standard library is required. Tokens/passwords are never printed.
"""

import datetime as dt
import email
import email.policy
import email.utils
import html
import http.cookiejar
import io
import json
import os
from pathlib import Path
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zipfile


PDF = b"%PDF-1.4\n1 0 obj <</Type /Catalog>> endobj\n%%EOF"


def docx_bytes(extra_entry=None):
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w") as archive:
        archive.writestr("[Content_Types].xml", '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"/>')
        archive.writestr("word/document.xml", '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body/></w:document>')
        if extra_entry:
            archive.writestr(extra_entry, b"test")
    return output.getvalue()


class Suite:
    def __init__(self):
        base = os.environ.get("API_BASE") or os.environ.get("SPRINT1_URL", "http://localhost:5135")
        self.base = base.rstrip("/")
        if self.base.endswith("/api"):
            self.base = self.base[:-4]
        self.hr_password = os.environ["SPRINT1_TEST_HR_PASSWORD"]
        self.admin_password = os.environ["SPRINT1_TEST_ADMIN_PASSWORD"]
        self.hr_email = os.environ.get("SPRINT1_TEST_HR_EMAIL", "hr@sprint1.local")
        self.admin_email = os.environ.get("SPRINT1_TEST_ADMIN_EMAIL", "admin@sprint12.local")
        default_mail = Path(__file__).resolve().parents[1] / "backend" / "CareerPortal.Api" / "App_Data" / "mail"
        self.mail_dir = Path(os.environ.get("SPRINT1_TEST_MAIL_DIR", str(default_mail)))
        self.tag = uuid.uuid4().hex[:12]
        self.checks = 0
        self.hr = self.client()
        self.admin = self.client()
        self.student = self.client()
        self.other = self.client()
        self.anon = self.client()
        self.today = dt.datetime.now(dt.timezone(dt.timedelta(hours=7))).date()
        self.school = "Smoke School " + self.tag
        self.major = "CNTT Smoke " + self.tag
        self.password = "TestOnly123!"

    @staticmethod
    def client():
        return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def check(self, condition, message):
        if not condition:
            raise AssertionError(message)
        self.checks += 1

    def req(self, client, method, path, data=None, expected=200, headers=None):
        body = data if isinstance(data, bytes) else json.dumps(data, ensure_ascii=False).encode("utf-8") if data is not None else None
        content_type = "application/octet-stream" if isinstance(data, bytes) else "application/json"
        request = urllib.request.Request(self.base + "/api" + path, data=body, method=method,
                                         headers={"Content-Type": content_type, **(headers or {})})
        try:
            response = client.open(request, timeout=35)
        except urllib.error.HTTPError as error:
            response = error
        raw = response.read()
        if response.status != expected:
            # Never include input bodies containing credentials or activation tokens.
            raise AssertionError(f"{method} {path}: expected {expected}, received {response.status}; {raw[:900]!r}")
        self.checks += 1
        try:
            return json.loads(raw)
        except (json.JSONDecodeError, UnicodeDecodeError):
            return raw

    def login(self, client, identity, password, expected=200):
        return self.req(client, "POST", "/auth/login", {"identity": identity, "password": password}, expected)

    def registration(self, prefix="student"):
        # 12 digits stay within validation and reduce collisions across reruns.
        phone = "09" + str(int(uuid.uuid4().hex[:12], 16) % 10**10).zfill(10)
        return {"name": prefix + " " + self.tag, "studentId": prefix.upper() + self.tag,
                "email": prefix + "." + self.tag + "@example.test", "phone": phone,
                "dateOfBirth": "2003-08-17", "school": self.school, "major": self.major,
                "password": self.password}

    def mail_token(self, recipient, parameter, identifier_key, identifier, timeout=30):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            paths = []
            for path in self.mail_dir.glob("*.eml"):
                try:
                    paths.append((path.stat().st_mtime, path))
                except OSError:
                    continue
            for _, path in sorted(paths, reverse=True):
                try:
                    message = email.message_from_bytes(path.read_bytes(), policy=email.policy.default)
                    if email.utils.parseaddr(message.get("To", ""))[1].lower() != recipient.lower():
                        continue
                    parts = message.walk() if message.is_multipart() else [message]
                    text = "\n".join(part.get_content() for part in parts if part.get_content_type() in ("text/plain", "text/html"))
                    for url in re.findall(r'''https?://[^\s<>"']+''', html.unescape(text)):
                        query = urllib.parse.parse_qs(urllib.parse.urlsplit(url).query)
                        if query.get(identifier_key) == [str(identifier)] and parameter in query:
                            self.checks += 1
                            return query[parameter][0]
                except (OSError, ValueError, UnicodeError, TypeError):
                    # Pickup mail can be observed while SMTP is still writing it.
                    continue
            time.sleep(0.5)
        raise AssertionError(f"No {parameter} invitation for tagged fixture in mail pickup directory {self.mail_dir}")

    def verify_registration_if_needed(self, profile):
        if not profile["emailVerified"]:
            token = self.mail_token(profile["email"], "verify", "profileId", profile["id"])
            self.req(self.anon, "POST", "/auth/verify-email", {"profileId": profile["id"], "token": "invalid"}, 400)
            self.req(self.anon, "POST", "/auth/verify-email", {"profileId": profile["id"], "token": token})
            self.req(self.anon, "POST", "/auth/verify-email", {"profileId": profile["id"], "token": token}, 400)

    def upload(self, profile_id, kind="CV", filename="smoke.pdf", content=PDF, expected=201):
        query = urllib.parse.urlencode({"type": kind, "fileName": filename})
        return self.req(self.student, "POST", f"/interns/{profile_id}/documents?{query}", content, expected)

    def workspace(self, profile_id):
        return self.req(self.student, "GET", f"/interns/{profile_id}/workspace")

    def sprint1(self):
        self.req(self.anon, "GET", "/hr/dashboard", expected=401)
        self.req(self.anon, "GET", "/accounts", expected=401)
        hr = self.login(self.hr, self.hr_email, self.hr_password)
        admin = self.login(self.admin, self.admin_email, self.admin_password)
        self.check(hr["role"] == "HR" and hr["href"] == "hr.html", "HR role/route incorrect")
        self.check(admin["role"] == "Admin" and admin["href"] == "admin.html", "Admin role/route incorrect")
        self.req(self.hr, "GET", "/accounts", expected=403)

        data = self.registration()
        for fields in ({"email": "invalid"}, {"phone": "abc"}, {"phone": ""}, {"name": ""}, {"password": "short"},
                       {"dateOfBirth": (self.today + dt.timedelta(days=2)).isoformat()}):
            self.req(self.anon, "POST", "/interns/register", {**data, **fields}, 400)
        profile = self.req(self.student, "POST", "/interns/register", data, 201)
        pid = profile["id"]
        self.req(self.anon, "POST", "/interns/register", data, 409)
        self.login(self.student, data["email"], data["password"])
        self.req(self.student, "GET", "/hr/dashboard", expected=403)
        self.req(self.student, "GET", "/HR/dashboard", expected=403)
        self.req(self.student, "POST", "/auth/logout", expected=403, headers={"Origin": "https://untrusted.example"})
        self.req(self.student, "POST", f"/interns/{pid}/applications", expected=400)
        self.verify_registration_if_needed(profile)

        hr_data = {k: v for k, v in self.registration("hrprofile").items() if k != "password"}
        hr_data.update(phone=None, status="Đang thực tập", startDate=(self.today - dt.timedelta(days=1)).isoformat(),
                       endDate=(self.today + dt.timedelta(days=30)).isoformat())
        self.req(self.hr, "POST", "/hr/profiles", {**hr_data, "dateOfBirth": None}, 400)
        self.req(self.hr, "POST", "/hr/profiles", {**hr_data, "startDate": None, "endDate": None}, 400)
        self.req(self.hr, "POST", "/hr/profiles", {**hr_data, "startDate": hr_data["endDate"], "endDate": hr_data["startDate"]}, 400)
        self.req(self.hr, "POST", "/hr/profiles", {**hr_data, "status": "Unknown"}, 400)
        created = self.req(self.hr, "POST", "/hr/profiles", hr_data, 201)
        self.check(created["dateOfBirth"] == hr_data["dateOfBirth"] and created["status"] == "Đang thực tập", "HR profile DOB/status missing")
        self.req(self.hr, "POST", "/hr/profiles", {**hr_data, "studentId": "DUP" + self.tag}, 409)
        review_query = urllib.parse.urlencode({"type": "CV", "fileName": "review-rejection.pdf"})
        review_document = self.req(self.hr, "POST", f"/interns/{created['id']}/documents?{review_query}", PDF, 201)
        review_reason = "Vui lòng bổ sung tài liệu hợp lệ " + self.tag
        rejected_document = self.req(self.hr, "PATCH", f"/hr/documents/{review_document['id']}",
                         {"status": "Từ chối", "note": review_reason})
        self.check(rejected_document["status"] == "Từ chối" and rejected_document["note"] == review_reason
               and rejected_document["reviewedBy"] and rejected_document["reviewedAt"],
               "Document rejection reason/reviewer metadata missing")
        self.req(self.hr, "GET", "/interns?page=0", expected=400)
        query = urllib.parse.urlencode({"search": self.tag, "school": self.school, "major": self.major, "pageSize": 1})
        first = self.req(self.hr, "GET", "/interns?" + query)
        second = self.req(self.hr, "GET", "/interns?" + query + "&page=2")
        self.check(first["total"] == 2 and len(first["items"]) == 1 and first["items"][0]["id"] != second["items"][0]["id"], "Search/filter/page results incorrect")
        pending = self.req(self.hr, "GET", "/interns?" + query + "&status=" + urllib.parse.quote("Chờ hồ sơ"))
        self.check(pending["total"] == 1 and pending["items"][0]["id"] == pid, "Status filter incorrect")
        update = {"name": "Updated " + self.tag, "email": data["email"], "phone": data["phone"], "school": self.school,
                  "major": self.major, "status": "Chờ hồ sơ", "startDate": None, "endDate": None}
        self.req(self.hr, "PUT", "/hr/profiles/2147483647", update, 404)
        self.req(self.hr, "PUT", f"/hr/profiles/{pid}", {**update, "email": created["email"]}, 409)
        self.req(self.hr, "PUT", f"/hr/profiles/{pid}", {**update, "phone": "wrong"}, 400)
        self.req(self.hr, "PUT", f"/hr/profiles/{pid}", update)
        self.check(self.workspace(pid)["profile"]["name"] == update["name"], "HR update not visible to intern")

        for filename, content in (("bad.exe", PDF), ("bad.pdf", b"not a PDF"), ("../bad.pdf", PDF),
                                  ("empty.pdf", b""), ("large.pdf", b"x" * (10 * 1024 * 1024 + 1)),
                                  ("bad.docx", b"not a ZIP"), ("macro.docx", docx_bytes("word/vbaProject.bin")),
                                  ("traversal.docx", docx_bytes("../bad.xml"))):
            self.upload(pid, filename=filename, content=content, expected=400)
        cv1 = self.upload(pid)
        cv2 = self.upload(pid)
        letter = self.upload(pid, "Đơn xin thực tập", "application.docx", docx_bytes())
        contract_query = urllib.parse.urlencode({"fileName": "contract.pdf", "startsAt": (self.today + dt.timedelta(days=1)).isoformat(),
                                                "expiresAt": (self.today + dt.timedelta(days=31)).isoformat()})
        self.req(self.hr, "POST", f"/hr/profiles/{pid}/contract?{contract_query}", PDF, 400)
        self.check(cv2["version"] == 2, "Document replacement version not incremented")
        versions = self.req(self.student, "GET", f"/interns/{pid}/documents")
        self.check(next(x for x in versions if x["id"] == cv1["id"])["isCurrent"] is False, "Superseded document still current")
        self.check(self.req(self.student, "GET", f"/documents/{cv2['id']}/file") == PDF, "Downloaded document bytes changed")
        self.req(self.student, "GET", f"/documents/{letter['id']}/preview", expected=400)
        self.req(self.student, "PATCH", f"/hr/documents/{cv2['id']}", {"status": "Đã duyệt"}, 403)
        self.req(self.hr, "PATCH", f"/hr/documents/{cv1['id']}", {"status": "Đã duyệt"}, 409)
        self.req(self.hr, "PATCH", f"/hr/documents/{cv2['id']}", {"status": "Từ chối"}, 400)
        application = self.req(self.student, "POST", f"/interns/{pid}/applications")
        self.req(self.hr, "PATCH", f"/hr/applications/{application['id']}", {"status": "Đã duyệt"}, 400)
        self.req(self.student, "POST", f"/interns/{pid}/applications", expected=409)
        self.upload(pid, expected=409)
        self.req(self.hr, "PATCH", f"/hr/documents/{cv2['id']}", {"status": "Đã duyệt"})
        work = self.workspace(pid)
        self.check(work["application"]["status"] == "Chờ duyệt", "Approving one document automatically approved the application")
        self.req(self.hr, "PATCH", f"/hr/documents/{letter['id']}", {"status": "Đã duyệt"})
        self.check(self.workspace(pid)["application"]["status"] == "Chờ duyệt", "Document reviews bypassed explicit application approval")
        aid = application["id"]
        self.req(self.hr, "PATCH", f"/hr/applications/{aid}", {"status": "Từ chối"}, 400)
        reason = "Bổ sung tài liệu cho lần nộp mới " + self.tag
        self.req(self.hr, "PATCH", f"/hr/applications/{aid}", {"status": "Từ chối", "note": reason})
        self.check(self.workspace(pid)["application"]["note"] == reason, "Application rejection reason missing")
        cv3 = self.upload(pid)
        letter2 = self.upload(pid, "Đơn xin thực tập", "application.docx", docx_bytes())
        resubmitted = self.req(self.student, "POST", f"/interns/{pid}/applications")
        self.check(resubmitted["id"] == aid and resubmitted["status"] == "Chờ duyệt" and resubmitted["note"] is None, "Resubmission did not reset pending review")
        self.req(self.hr, "PATCH", f"/hr/documents/{cv3['id']}", {"status": "Đã duyệt"})
        self.req(self.hr, "PATCH", f"/hr/documents/{letter2['id']}", {"status": "Đã duyệt"})
        self.check(self.workspace(pid)["application"]["status"] == "Chờ duyệt", "Resubmission automatically approved by document review")
        self.req(self.hr, "PATCH", f"/hr/applications/{aid}", {"status": "Đã duyệt"})
        self.req(self.hr, "PATCH", f"/hr/applications/{aid}", {"status": "Đã duyệt"}, 409)

        self.req(self.hr, "POST", f"/hr/profiles/{pid}/contract?{contract_query}", b"", 400)
        contract = self.req(self.hr, "POST", f"/hr/profiles/{pid}/contract?{contract_query}", PDF, 201)
        replacement_contract = self.req(self.hr, "POST", f"/hr/profiles/{pid}/contract?{contract_query}", PDF, 201)
        self.check(replacement_contract["version"] == 2, "Contract replacement version not incremented")
        self.req(self.student, "POST", f"/contracts/{contract['id']}/confirm", expected=409)
        contract = replacement_contract
        self.req(self.hr, "POST", f"/contracts/{contract['id']}/confirm", expected=403)
        other_data = self.registration("other")
        other_profile = self.req(self.other, "POST", "/interns/register", other_data, 201)
        self.login(self.other, other_data["email"], self.password)
        self.req(self.other, "GET", f"/interns/{pid}/workspace", expected=403)
        self.req(self.other, "GET", f"/documents/{contract['id']}/file", expected=403)
        self.req(self.other, "POST", f"/contracts/{contract['id']}/confirm", expected=403)
        self.req(self.student, "POST", f"/contracts/{contract['id']}/confirm")
        self.req(self.student, "POST", f"/contracts/{contract['id']}/confirm", expected=409)
        final = self.workspace(pid)
        self.check(final["profile"]["status"] == "Chờ bắt đầu", "Future signed contract status incorrect")
        decisions = [x for x in final["reviewHistory"] if x["targetType"] == "Hồ sơ"]
        self.check({x["status"] for x in decisions} == {"Từ chối", "Đã duyệt"}, "Application review history missing decisions")
        deadline = time.monotonic() + 30
        while True:
            jobs = self.req(self.hr, "GET", "/hr/email-status")
            decisions = [x for x in jobs if x["eventKey"].startswith(f"decision:{aid}:")]
            if len(decisions) == 2 and all(x["status"] == "Sent" for x in decisions):
                break
            if time.monotonic() >= deadline:
                raise AssertionError("Decision emails were not delivered exactly once for both submissions")
            time.sleep(0.5)
        self.check(len(decisions) == 2, "Unexpected duplicate application decision mail")
        print(f"PASS Sprint 1 profile/document/application checks; fixture tag {self.tag}", flush=True)
        return created

    def activate(self, account_id, recipient, password=None):
        token = self.mail_token(recipient, "token", "accountId", account_id)
        body = {"accountId": account_id, "token": token, "password": password or self.password}
        self.req(self.anon, "POST", "/auth/activate", {**body, "token": "wrong"}, 400)
        self.req(self.anon, "POST", "/auth/activate", {**body, "password": "short"}, 400)
        self.req(self.anon, "POST", "/auth/activate", body)
        self.req(self.anon, "POST", "/auth/activate", body, 400)
        return token

    def accounts(self, hr_created):
        listing = self.req(self.admin, "GET", "/accounts")
        self.check({x["name"] for x in listing["roles"]} == {"Admin", "HR", "Mentor", "Intern"}, "Persistent roles missing")
        self.check({x["key"] for x in listing["permissions"]} >= {"accounts.manage", "roles.manage", "profiles.read"}, "Permission catalog missing")
        intern_account = next(x for x in listing["items"] if x["profileId"] == hr_created["id"])
        self.check(intern_account["requiresActivation"] is True, "HR-created profile missing activation invitation")
        self.login(self.other, hr_created["email"], "17082003", 401)
        self.activate(intern_account["id"], hr_created["email"])
        intern_identity = self.login(self.other, hr_created["email"], self.password)
        self.check(intern_identity["profileId"] == hr_created["id"] and intern_identity["href"] == "thuc-tap-sinh.html", "Activated HR profile not linked to account")

        data = {"name": "Invited HR " + self.tag, "email": "account." + self.tag + "@example.test", "roles": ["HR"], "profileId": None, "isActive": True}
        self.req(self.admin, "POST", "/accounts", {**data, "roles": []}, 400)
        self.req(self.admin, "POST", "/accounts", {**data, "roles": ["Unknown"]}, 400)
        self.req(self.admin, "POST", "/accounts", {**data, "roles": ["Intern"]}, 400)
        self.req(self.admin, "POST", "/accounts", {**data, "roles": ["Intern"], "profileId": hr_created["id"], "email": hr_created["email"]}, 400)
        created = self.req(self.admin, "POST", "/accounts", data, 201)
        account = created["account"]
        account_id = account["id"]
        self.check(account["isActive"] is True, "New account should be active")
        session = self.client()
        self.check(account["requiresActivation"] is True, "New account can bypass activation")
        self.login(session, data["email"], self.password, 401)
        self.req(self.admin, "POST", "/accounts", data, 400)
        first_token = self.activate(account_id, data["email"])
        identity = self.login(session, data["email"], self.password)
        self.check(identity["accountId"] == account_id and identity["role"] == "HR", "Activated account identity incorrect")
        self.req(session, "GET", "/hr/dashboard")

        edited = {**data, "name": "Updated Account " + self.tag, "email": "updated.account." + self.tag + "@example.test", "roles": ["HR", "Mentor"]}
        self.req(self.admin, "PUT", f"/accounts/{account_id}", edited)
        self.check(self.req(session, "GET", "/auth/me")["name"] == edited["name"], "Existing session did not receive updated account name")
        self.login(self.client(), data["email"], self.password, 401)
        self.check(set(self.login(self.client(), edited["email"], self.password)["roles"]) == {"HR", "Mentor"}, "Account email/role update ineffective")
        mentor_only = {**edited, "roles": ["Mentor"]}
        self.req(self.admin, "PUT", f"/accounts/{account_id}", mentor_only)
        self.req(session, "GET", "/hr/dashboard", expected=403)
        self.check(self.req(session, "GET", "/auth/me")["href"] == "mentor.html", "Existing session retained removed HR role")

        roles = self.req(self.admin, "GET", "/accounts")["roles"]
        original = next(x["permissions"] for x in roles if x["name"] == "Mentor")
        original_copy = list(original)
        try:
            granted = sorted(set(original_copy) | {"profiles.read"})
            self.req(self.admin, "PUT", "/roles/Mentor/permissions", {"permissions": granted})
            self.req(session, "GET", "/hr/dashboard")
            self.check("profiles.read" in self.req(session, "GET", "/auth/me")["permissions"], "Permission grant did not reach existing session")
            self.req(self.admin, "PUT", "/roles/Mentor/permissions", {"permissions": [x for x in granted if x != "profiles.read"]})
            self.req(session, "GET", "/hr/dashboard", expected=403)
            self.check("profiles.read" not in self.req(session, "GET", "/auth/me")["permissions"], "Revoked permission retained in existing session")
        finally:
            self.req(self.admin, "PUT", "/roles/Mentor/permissions", {"permissions": original_copy})
        self.req(self.admin, "PUT", "/roles/Mentor/permissions", {"permissions": ["unknown.permission"]}, 400)
        self.req(self.admin, "PUT", "/roles/Admin/permissions", {"permissions": []}, 400)

        self.req(self.admin, "PUT", f"/accounts/{account_id}", {**mentor_only, "isActive": False})
        disabled_account = next(x for x in self.req(self.admin, "GET", "/accounts")["items"] if x["id"] == account_id)
        self.check(disabled_account["isActive"] is False, "Disabled account still active")
        self.req(session, "GET", "/auth/me", expected=401)
        self.login(self.client(), edited["email"], self.password, 401)
        self.req(self.admin, "POST", f"/accounts/{account_id}/activation", expected=400)
        self.req(self.admin, "PUT", f"/accounts/{account_id}", mentor_only)
        self.login(session, edited["email"], self.password)
        self.req(self.admin, "POST", f"/accounts/{account_id}/activation")
        self.req(session, "GET", "/auth/me", expected=401)
        self.login(self.client(), edited["email"], self.password, 401)
        self.req(self.admin, "POST", f"/accounts/{account_id}/activation", expected=400)
        reset_token = self.activate(account_id, edited["email"], "ResetOnly456!")
        self.check(first_token != reset_token, "Password reset reused original activation token")
        self.login(session, edited["email"], "ResetOnly456!")
        self.req(self.admin, "DELETE", f"/accounts/{account_id}", expected=204)
        self.req(session, "GET", "/auth/me", expected=401)
        self.login(self.client(), edited["email"], "ResetOnly456!", 401)
        final_account = next(x for x in self.req(self.admin, "GET", "/accounts")["items"] if x["id"] == account_id)
        self.check(final_account["isActive"] is False, "Soft deleted account still active")
        print("PASS account activation/RBAC/revocation/disable/password-reset checks", flush=True)

    def run(self):
        created = self.sprint1()
        self.accounts(created)
        print(f"PASS: {self.checks} HTTP and state assertions. Fixtures tagged {self.tag}; business history retained.", flush=True)


if __name__ == "__main__":
    Suite().run()
