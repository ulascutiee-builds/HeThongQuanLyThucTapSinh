"""Integration tests against a dedicated, disposable LocalDB database.
Run API on 5148 with BootstrapToken=local-test-bootstrap-20261001 and SMTP localhost:2526.
"""
import http.cookiejar
import io
import json
import re
import socketserver
import threading
import time
import urllib.error
import urllib.request
import zipfile
from pathlib import Path
from datetime import datetime, timezone, timedelta

BASE = 'http://localhost:5148'
suffix = str(int(time.time()))
password = 'LocalTesting-2026!'
checks = []
mail = []

class Smtp(socketserver.StreamRequestHandler):
    def handle(self):
        self.wfile.write(b'220 test.local ESMTP\r\n')
        while line := self.rfile.readline():
            command = line.decode().strip().upper()
            if command.startswith(('EHLO','HELO')): self.wfile.write(b'250 test.local\r\n')
            elif command == 'DATA':
                self.wfile.write(b'354 Go\r\n')
                body = b''
                while (part := self.rfile.readline()) != b'.\r\n':
                    if not part: break
                    body += part
                mail.append(body)
                self.wfile.write(b'250 OK\r\n')
            elif command == 'QUIT':
                self.wfile.write(b'221 Bye\r\n'); break
            else: self.wfile.write(b'250 OK\r\n')

class Client:
    def __init__(self): self.http = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def call(self, method, path, data=None, expected=200, headers=None):
        h = headers or {}
        if data is not None and not isinstance(data, bytes): h['Content-Type']='application/json'; data=json.dumps(data).encode()
        req = urllib.request.Request(BASE+path, data=data, headers=h, method=method)
        try:
            with self.http.open(req) as r: status=r.status; body=r.read()
        except urllib.error.HTTPError as e: status=e.code; body=e.read()
        assert status==expected, f'{method} {path}: expected {expected}, got {status}: {body[:1800]}'
        checks.append(f'{method} {path} => {status}')
        try: return json.loads(body)
        except (ValueError,UnicodeDecodeError): return body

server=socketserver.TCPServer(('127.0.0.1',2526), Smtp)
threading.Thread(target=server.serve_forever,daemon=True).start()
admin,hr,mentor,intern,other,anon=[Client() for _ in range(6)]
anon.call('GET','/api/hr/dashboard',expected=401)
anon.call('GET','/api/work/tasks',expected=401)
admin.call('POST','/api/auth/bootstrap',{'name':'Test Admin','email':'admin@test.local','password':password,'role':'Admin'},headers={'X-Bootstrap-Token':'local-test-bootstrap-20261001'})
admin.call('POST','/api/auth/login',{'identity':'admin@test.local','password':password})
def account(role):
    email=f'{role.lower()}-{suffix}@test.local'
    result=admin.call('POST','/api/accounts',{'name':role+' Demo','email':email,'password':password,'role':role})
    return result['id'],email
hr_id,hr_email=account('HR'); mentor_id,mentor_email=account('Mentor')
hr.call('POST','/api/auth/login',{'identity':hr_email,'password':password})
mentor.call('POST','/api/auth/login',{'identity':mentor_email,'password':password})
def register(client,label):
    email=f'{label}-{suffix}@test.local'
    p=client.call('POST','/api/interns/register',{'name':label,'studentId':label+suffix,'email':email,'school':'ICTU','major':'CNTT','password':password},201)
    client.call('POST','/api/interns/login',{'identity':email,'password':password})
    return p['id']
pid=register(intern,'Intern'); pid2=register(other,'Other')
intern.call('GET',f'/api/interns/{pid2}/workspace',expected=403)
intern.call('GET','/api/hr/dashboard',expected=403)
intern.call('POST',f'/api/interns/{pid}/submit',expected=400)
jobs=admin.call('GET','/api/admin/mail')
token=re.search(r'verify=([A-F0-9]+)',next(j['body'] for j in jobs if f'profileId={pid}' in j['body'])).group(1)
anon.call('POST','/api/auth/verify-email',{'profileId':pid,'token':token})
anon.call('POST','/api/auth/verify-email',{'profileId':pid,'token':token},400)
for kind in ['CV','Đơn xin thực tập']:
    intern.call('POST',f'/api/interns/{pid}/documents?fileName=test.pdf&type='+urllib.parse.quote(kind),b'%PDF-1.4 test',201,{'Content-Type':'application/pdf'})
intern.call('POST',f'/api/interns/{pid}/submit')
dashboard=hr.call('GET','/api/hr/dashboard')
application=next(x for x in dashboard['applications'] if x['profileId']==pid)
hr.call('PATCH',f"/api/hr/applications/{application['id']}",{'status':'Đã duyệt','note':'Đạt yêu cầu'})
hr.call('POST',f'/api/hr/profiles/{pid}/contract?fileName=contract.pdf',b'%PDF-1.4 test',201,{'Content-Type':'application/pdf'})
workspace=intern.call('GET',f'/api/interns/{pid}/workspace')
contract=next(x for x in workspace['documents'] if x['type']=='Hợp đồng thực tập')
other.call('GET',f"/api/documents/{contract['id']}/file",expected=403)
intern.call('POST',f"/api/interns/{pid}/documents/{contract['id']}/confirm-contract")
start='2026-10-01T08:00:00+07:00'; end='2026-12-31T17:00:00+07:00'
def create(client,kind,**data): return client.call('POST','/api/work/'+kind,{'title':'Test '+kind,**data})
program=create(hr,'programs',department='IT',capacity=10,start=start,end=end)
guide=create(hr,'mentors',department='IT',capacity=mentor_id,amount=1)
assignment=create(hr,'assignments',profileId=pid,programId=program['id'],mentorId=guide['id'])
create_data={'title':'Duplicate','profileId':pid,'programId':program['id'],'mentorId':guide['id']}
hr.call('POST','/api/work/assignments',create_data,400)
hr.call('POST','/api/work/assignments',{**create_data,'profileId':pid2},400)
task=create(mentor,'tasks',profileId=pid,end=end,detail='Build feature')
mentor.call('POST','/api/work/tasks',{'title':'No access','profileId':pid2,'end':end},400)
intern.call('POST',f"/api/work/tasks/{task['id']}/action",{'action':'progress','progress':101},400)
intern.call('POST',f"/api/work/tasks/{task['id']}/action",{'action':'progress','progress':100})
other.call('POST',f"/api/work/tasks/{task['id']}/action",{'action':'progress','progress':20},404)
report=create(intern,'reports',profileId=pid,start='2026-09-28T00:00:00+07:00',detail='Tuần 1')
intern.call('POST','/api/work/reports',{'title':'Trùng','profileId':pid,'start':'2026-09-28T00:00:00+07:00'},400)
intern.call('POST',f"/api/work/reports/{report['id']}/attachment?fileName=proof.pdf",b'%PDF-1.4 proof')
mentor.call('POST',f"/api/work/reports/{report['id']}/action",{'action':'feedback','feedback':'Cần bổ sung kiểm thử'})
create(mentor,'evaluations',profileId=pid,amount=8,progress=9,detail='Tiến bộ tốt')
create(hr,'shifts',profileId=pid,start=start,end='2026-10-01T17:00:00+07:00')
intern.call('POST','/api/attendance/check-in')
intern.call('POST','/api/attendance/check-in',expected=400)
intern.call('POST','/api/attendance/check-out')
leave=create(intern,'leave',profileId=pid,start='2026-10-02T08:00:00+07:00',end='2026-10-02T17:00:00+07:00')
hr.call('POST',f"/api/work/leave/{leave['id']}/action",{'action':'approve'})
allowance=create(hr,'allowances',profileId=pid,amount=1000000,start=start)
hr.call('POST',f"/api/work/allowances/{allowance['id']}/action",{'action':'paid'})
support=create(intern,'support',profileId=pid,detail='Xin giấy xác nhận')
hr.call('POST',f"/api/work/support/{support['id']}/action",{'action':'close','feedback':'Đã cấp'})
create(mentor,'meetings',profileId=pid,start=start,end='2026-10-01T09:00:00+07:00')
notifications=intern.call('GET','/api/work/notifications')
assert notifications
intern.call('POST',f"/api/work/notifications/{notifications[0]['id']}/action",{'action':'read'})
other_tasks=other.call('GET','/api/work/tasks'); assert not other_tasks
stats=hr.call('GET','/api/reports/summary');assert stats['total']==2
xlsx=hr.call('GET','/api/reports/export.xlsx')
with zipfile.ZipFile(io.BytesIO(xlsx)) as z: assert b'ICTU' in z.read('xl/worksheets/sheet1.xml')
intern.call('GET','/api/reports/export.xlsx',expected=403)
hrm=[{'employeeCode':'EX001','name':'External','email':'ext@test.local','school':'ICTU','major':'IT'}]
admin.call('POST','/api/admin/integrations/hrm',hrm)
admin.call('POST','/api/admin/integrations/hrm',hrm)
event={'eventId':'CARD01','studentId':'HRM-EX001','start':datetime.now(timezone.utc).isoformat(),'end':None}
assert admin.call('POST','/api/admin/integrations/attendance',event)['duplicate'] is False
assert admin.call('POST','/api/admin/integrations/attendance',event)['duplicate'] is True
backup=admin.call('POST','/api/admin/backups')
assert admin.call('GET','/api/admin/backups/'+backup['file'])
assert admin.call('GET','/api/admin/audit')
intern.call('GET','/api/admin/audit',expected=403)
admin.call('POST',f'/api/admin/interns/{pid2}/account',{'password':'','active':False})
other.call('GET','/api/auth/me',expected=401)
admin.call('POST','/api/work/tasks',{'title':'CSRF','profileId':pid,'end':end},403,{'Origin':'https://untrusted.example'})
deadline=time.time()+45
while time.time()<deadline and not mail: time.sleep(1)
assert mail,'SMTP worker did not deliver to local test sink'
result={'passed':len(checks),'checks':checks,'smtpMessages':len(mail)}
Path(__file__).with_name('test-result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passed':len(checks),'smtpMessages':len(mail)}))
server.shutdown()
