"""Integration checks against a running Sprint 1 demo. Creates uniquely named test records; never deletes data."""
import os,json,urllib.request,urllib.error,http.cookiejar,uuid,time,re,email,email.policy,io,zipfile
from pathlib import Path
BASE=os.environ.get('SPRINT1_URL','http://localhost:5135')
password=os.environ['SPRINT1_TEST_HR_PASSWORD']
tag=uuid.uuid4().hex[:10];checks=0
def client():return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
hr=client();a=client();b=client();anon=client()
def req(c,method,path,data=None,expected=200):
 global checks
 body=data if isinstance(data,bytes) else json.dumps(data).encode() if data is not None else None
 request=urllib.request.Request(BASE+'/api'+path,data=body,method=method,headers={'Content-Type':'application/octet-stream' if isinstance(data,bytes) else 'application/json'})
 try:r=c.open(request)
 except urllib.error.HTTPError as e:r=e
 raw=r.read();assert r.status==expected,(method,path,r.status,raw[:800]);checks+=1
 try:return json.loads(raw)
 except:return raw
req(anon,'GET','/hr/dashboard',expected=401)
req(hr,'POST','/auth/login',{'identity':'hr@sprint1.local','password':password})
profile={'name':'Test Sprint '+tag,'studentId':'TEST'+tag,'email':tag+'@example.test','phone':'09'+str(int(tag,16))[-8:].zfill(8),'school':'ICTU Test','major':'CNTT Test','password':'TestOnly123!'}
p=req(a,'POST','/interns/register',profile,201);pid=p['id']
req(hr,'POST',f'/hr/profiles/{pid}/contract?fileName=contract.pdf',b'',400)
req(a,'POST','/interns/register',profile,409)
req(a,'POST','/interns/login',{'identity':profile['email'],'password':profile['password']})
req(a,'GET','/hr/dashboard',expected=403)
req(a,'GET','/HR/dashboard',expected=403)
req(a,'POST',f'/interns/{pid}/applications',expected=400)
bad=dict(profile,email='invalid');req(hr,'POST','/interns',bad,400)
req(hr,'POST','/interns',dict(profile,name=''),400)
req(hr,'POST','/interns',dict(profile,phone='abc'),400)
req(hr,'POST','/interns',dict(profile,startDate='2026-10-02',endDate='2026-10-01'),400)
created=req(hr,'POST','/interns',dict(profile,studentId='HR'+tag,email='hr'+tag+'@example.test',phone=None,status='Chờ hồ sơ'),201)
assert req(hr,'GET',f"/interns/{created['id']}/workspace")['profile']['email']=='hr'+tag+'@example.test';checks+=1
bad=dict(profile,studentId='other'+tag,email='other'+tag+'@example.test');req(hr,'POST','/interns',bad,409)
req(hr,'PUT','/hr/profiles/2147483647',dict(profile,status='Chờ hồ sơ'),404)
req(hr,'GET','/interns?page=0',expected=400)
q=req(hr,'GET','/interns?'+urllib.parse.urlencode({'search':tag,'school':'ICTU Test','major':'CNTT Test','status':'Chờ hồ sơ','pageSize':1}));assert q['total']==2 and len(q['items'])==1;checks+=1
page2=req(hr,'GET','/interns?'+urllib.parse.urlencode({'search':tag,'page':2,'pageSize':1}));assert page2['items'][0]['id']!=q['items'][0]['id'];checks+=1
update=dict(profile,name='Updated '+tag,status='Chờ hồ sơ');req(hr,'PUT',f'/hr/profiles/{pid}',update)
req(hr,'PUT',f'/hr/profiles/{pid}',dict(update,email='hr'+tag+'@example.test'),409)
req(hr,'PUT',f'/hr/profiles/{pid}',dict(update,phone='wrong'),400)
assert req(a,'GET',f'/interns/{pid}/workspace')['profile']['name']==update['name'];checks+=1
pdf=b'%PDF-1.4\n1 0 obj <</Type /Catalog>> endobj\n%%EOF'
def upload(c,t='CV',name='test.pdf',body=pdf,status=201):return req(c,'POST',f'/interns/{pid}/documents?'+urllib.parse.urlencode({'type':t,'fileName':name}),body,status)
upload(a,name='bad.exe',status=400);upload(a,body=b'not pdf',status=400);upload(a,name='../bad.pdf',status=400)
upload(a,body=b'x'*(10*1024*1024+1),status=400)
doc=upload(a);replacement=upload(a);assert replacement['version']==2;checks+=1
docx=io.BytesIO()
with zipfile.ZipFile(docx,'w') as z:
 z.writestr('[Content_Types].xml','<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"/>')
 z.writestr('word/document.xml','<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>')
letter=upload(a,'Đơn xin thực tập','application.docx',docx.getvalue())
assert req(a,'GET',f"/documents/{replacement['id']}/file")==pdf;checks+=1
req(a,'PATCH',f"/hr/documents/{replacement['id']}",{'status':'Đã duyệt'},403)
req(hr,'PATCH',f"/hr/documents/{doc['id']}",{'status':'Đã duyệt'},409)
req(hr,'PATCH',f"/hr/documents/{replacement['id']}",{'status':'Từ chối'},400)
req(hr,'PATCH',f"/hr/documents/{replacement['id']}",{'status':'Đã duyệt'})
req(hr,'POST',f'/hr/profiles/{pid}/contract?fileName=contract.pdf',b'',400)
req(hr,'PATCH',f"/hr/documents/{letter['id']}",{'status':'Đã duyệt'})
maildir=Path(__file__).resolve().parents[1]/'backend/CareerPortal.Api/App_Data/mail'
token=None
for _ in range(15):
 for path in maildir.glob('*.eml'):
  msg=email.message_from_bytes(path.read_bytes(),policy=email.policy.default)
  if msg['To']==profile['email']:
   body=msg.get_content();m=re.search(r'verify=([A-F0-9]+)',body)
   if m:token=m[1];break
 if token:break
 time.sleep(1)
assert token,'Verification mail missing';checks+=1
req(anon,'POST','/auth/verify-email',{'profileId':pid,'token':token})
app=req(a,'POST',f'/interns/{pid}/applications')
req(a,'POST',f'/interns/{pid}/applications',expected=409)
upload(a,status=409)
req(hr,'PATCH',f"/hr/applications/{app['id']}",{'status':'Từ chối'},400)
req(hr,'PATCH',f"/hr/applications/{app['id']}",{'status':'Từ chối','note':'Cần bổ sung thông tin'})
req(a,'POST',f'/interns/{pid}/applications')
req(hr,'PATCH',f"/hr/applications/{app['id']}",{'status':'Đã duyệt'})
req(hr,'PATCH',f"/hr/applications/{app['id']}",{'status':'Đã duyệt'},409)
contract=req(hr,'POST',f'/hr/profiles/{pid}/contract?fileName=contract.pdf',pdf,201)
req(hr,'POST',f"/contracts/{contract['id']}/confirm",expected=403)
other=dict(profile,studentId='B'+tag,email='b'+tag+'@example.test',phone=None);bp=req(b,'POST','/interns/register',other,201)
req(b,'POST','/interns/login',{'identity':other['email'],'password':other['password']})
req(b,'POST',f"/interns/{bp['id']}/documents?type=CV&fileName=test.pdf",pdf,400)
req(b,'POST',f"/interns/{bp['id']}/documents?type=Đơn xin thực tập&fileName=test.pdf",pdf,400)
req(b,'POST',f"/interns/{bp['id']}/applications",expected=400)
req(b,'GET',f'/interns/{pid}/workspace',expected=403)
req(b,'GET',f"/documents/{contract['id']}/file",expected=403)
req(b,'POST',f"/contracts/{contract['id']}/confirm",expected=403)
req(a,'POST',f"/contracts/{contract['id']}/confirm")
req(a,'POST',f"/contracts/{contract['id']}/confirm",expected=409)
workspace=req(a,'GET',f'/interns/{pid}/workspace');assert workspace['profile']['status']=='Đang thực tập' and len(workspace['reviewHistory'])==4;checks+=1
for path in ['/work/tasks','/admin/backups','/programs','/mentors','/attendance']:
 req(hr,'GET',path,expected=404)
for _ in range(15):
 jobs=req(hr,'GET','/hr/email-status');decisions=[x for x in jobs if x['eventKey'].startswith(f"decision:{app['id']}:")]
 if len(decisions)==2 and all(x['status']=='Sent' for x in decisions):break
 time.sleep(1)
assert len(decisions)==2 and all(x['status']=='Sent' for x in decisions);checks+=1
print(f'PASS: {checks} assertions / HTTP checks. Profile ID {pid}; tag {tag}. Data persisted in Sprint1 database.')
