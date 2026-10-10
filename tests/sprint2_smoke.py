"""Sprint 2 integration tests. Use an isolated LocalDB database and pickup email mode."""
import os, json, uuid, time, re, email, email.policy, urllib.request, urllib.error, http.cookiejar
from pathlib import Path
from datetime import datetime, timedelta, timezone
BASE=os.environ.get('SPRINT1_URL','http://localhost:5135')
MAIL=Path(os.environ.get('SPRINT1_TEST_MAIL_DIR',str(Path(__file__).resolve().parents[1]/'backend/CareerPortal.Api/App_Data/mail')))
TAG=uuid.uuid4().hex[:10]
checks=0
def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
hr,admin,intern,other,mentor,mentor2,anon=(client() for _ in range(7))
def req(c,method,path,data=None,expected=200):
    global checks
    body=json.dumps(data).encode() if data is not None else None
    request=urllib.request.Request(BASE+'/api'+path,data=body,method=method,headers={'Content-Type':'application/json'})
    try: response=c.open(request,timeout=20)
    except urllib.error.HTTPError as exc: response=exc
    raw=response.read()
    assert response.status==expected,(method,path,response.status,raw[:1500])
    checks+=1
    return json.loads(raw) if raw else None
def check(condition):
    global checks
    assert condition
    checks+=1
def activation(account_id,address):
    for _ in range(50):
        for path in MAIL.glob('*.eml'):
            msg=email.message_from_bytes(path.read_bytes(),policy=email.policy.default)
            if msg['To']!=address: continue
            body=msg.get_content()
            found=re.search(r'accountId='+str(account_id)+r'(?:&amp;|&)token=([A-F0-9]+)',body)
            if found:return found[1]
        time.sleep(.2)
    raise AssertionError('Activation email missing '+address)
req(hr,'POST','/auth/login',{'identity':'hr@sprint1.local','password':os.environ['SPRINT1_TEST_HR_PASSWORD']})
req(admin,'POST','/auth/login',{'identity':'admin@sprint12.local','password':os.environ['SPRINT1_TEST_ADMIN_PASSWORD']})
now=datetime.now(timezone(timedelta(hours=7)));today=now.date();yesterday=today-timedelta(days=1);tomorrow=today+timedelta(days=1)
start=(today-timedelta(days=2)).isoformat();end=(today+timedelta(days=10)).isoformat()
def register(c,suffix):
    p={'name':'Sprint2 '+TAG+suffix,'studentId':'S2'+TAG,'email':TAG+suffix+'@example.test','phone':'09'+str(int(TAG[:7],16)).zfill(8)[-8:-1]+suffix,'dateOfBirth':'2004-01-02','school':'University Test','major':'CNTT','password':'TestIntern123!'}
    # Separate, valid 10-digit phone numbers.
    p['phone']='09'+str(int(TAG[:7],16)%10000000).zfill(7)+suffix
    result=req(c,'POST','/interns/register',p,201)
    req(c,'POST','/interns/login',{'identity':p['email'],'password':p['password']})
    return result['id']
pid=register(intern,'1');otherid=register(other,'2')
req(anon,'GET','/programs',expected=401);req(intern,'POST','/departments',{'name':'Forbidden'},403)
dep=req(hr,'POST','/departments',{'name':'IT '+TAG},201)['id']
req(hr,'POST','/departments',{'name':'IT '+TAG},409)
dept2=req(hr,'POST','/departments',{'name':'HR '+TAG},201)['id']
program={'name':'Program '+TAG,'departmentId':dep,'description':'Integration tests','capacity':1,'startDate':start,'endDate':end,'status':'Active'}
req(hr,'POST','/programs',dict(program,endDate=start,startDate=end),400)
req(hr,'POST','/programs',dict(program,departmentId=2147483647),400)
req(intern,'POST','/programs',program,403)
prid=req(hr,'POST','/programs',program,201)['id']
spare=req(hr,'POST','/programs',dict(program,name='Delete '+TAG),201)['id'];req(hr,'DELETE',f'/programs/{spare}',expected=204)
req(hr,'DELETE',f'/programs/{spare}',expected=404)
mentors=[]
for suffix in ['a','b']:
    address=TAG+suffix+'mentor@example.test'
    result=req(admin,'POST','/accounts',{'name':'Mentor '+suffix+TAG,'email':address,'roles':['Mentor'],'isActive':True},201)
    uid=result['account']['id'];token=activation(uid,address)
    req(anon,'POST','/auth/activate',{'accountId':uid,'token':token,'password':'TestMentor123!'})
    if suffix=='a':req(mentor,'POST','/auth/login',{'identity':address,'password':'TestMentor123!'})
    else:req(mentor2,'POST','/auth/login',{'identity':address,'password':'TestMentor123!'})
    mid=req(hr,'POST','/mentors',{'userId':uid,'departmentId':dep,'capacity':1},201)['id'];mentors.append((uid,mid))
req(hr,'POST','/mentors',{'userId':mentors[0][0],'departmentId':dep,'capacity':1},409)
assignment=req(hr,'POST','/assignments',{'profileId':pid,'programId':prid,'mentorId':mentors[0][1]},201)['id']
check(len(req(intern,'GET','/assignments'))==1);check(req(other,'GET','/assignments')==[]);check(len(req(mentor,'GET','/assignments'))==1)
req(hr,'POST','/assignments',{'profileId':pid,'programId':prid,'mentorId':mentors[0][1]},409)
req(hr,'POST','/assignments',{'profileId':otherid,'programId':prid,'mentorId':mentors[0][1]},409)
req(intern,'PUT',f'/assignments/{assignment}',{'mentorId':mentors[1][1]},403)
req(hr,'PUT',f'/assignments/{assignment}',{'mentorId':mentors[1][1]})
check(req(mentor,'GET','/assignments')==[]);check(len(req(mentor2,'GET','/assignments'))==1)
req(hr,'PUT',f'/assignments/{assignment}',{'mentorId':mentors[0][1]})
check(len(req(mentor,'GET','/assignments'))==1);check(req(mentor2,'GET','/assignments')==[])
evaluation_payload={'profileId':pid,'programId':prid,'skills':5,'attitude':4,'communication':3,'teamwork':None,'comment':'Evaluation '+TAG}
req(mentor,'POST','/evaluations',evaluation_payload)
mentor_evaluation=next(item for item in req(mentor,'GET','/evaluations') if item['profileId']==pid)
intern_evaluation=next(item for item in req(intern,'GET','/evaluations') if item['profileId']==pid)
check(mentor_evaluation['average']==4.0 and intern_evaluation['average']==4.0)
check((intern_evaluation['skills'],intern_evaluation['attitude'],intern_evaluation['communication'])==(5,4,3))
summary=next(group for group in req(hr,'GET','/evaluations/summary') if group['programId']==prid)
check(summary['count']==1 and summary['average']==4.0 and summary['department']=='IT '+TAG)
check(summary['items'][0]['average']==4.0)
evaluation_payload={**evaluation_payload,'teamwork':2}
req(mentor,'POST','/evaluations',evaluation_payload)
mentor_evaluation=next(item for item in req(mentor,'GET','/evaluations') if item['profileId']==pid)
intern_evaluation=next(item for item in req(intern,'GET','/evaluations') if item['profileId']==pid)
summary=next(group for group in req(hr,'GET','/evaluations/summary') if group['programId']==prid)
check(mentor_evaluation['average']==3.5 and intern_evaluation['average']==3.5 and summary['average']==3.5 and summary['items'][0]['average']==3.5)
req(hr,'PUT',f'/programs/{prid}',dict(program,departmentId=dept2),400)
req(hr,'PUT',f'/programs/{prid}',dict(program,startDate=(today-timedelta(days=1)).isoformat()))
check(req(intern,'GET',f'/interns/{pid}/workspace')['profile']['startDate']==(today-timedelta(days=1)).isoformat())
week_start=today-timedelta(days=today.weekday())
on_time_payload={'weekStart':week_start.isoformat(),'summary':'On-time report '+TAG,'blockers':'','evidenceUrl':'https://example.test/evidence/on-time'}
on_time=req(intern,'POST','/reports/weekly',on_time_payload,201)
check(on_time['status']=='Submitted' and on_time['weekStart']==week_start.isoformat() and on_time['submittedAt'] is not None)
replacement_payload={**on_time_payload,'summary':'Replacement report '+TAG,'evidenceUrl':'https://example.test/evidence/replacement'}
replaced=req(intern,'PUT',f"/reports/weekly/{on_time['id']}",replacement_payload)
check(replaced['id']==on_time['id'] and replaced['summary']==replacement_payload['summary'] and replaced['status']=='Submitted')
req(intern,'POST','/reports/weekly',on_time_payload,409)
late_week=week_start-timedelta(days=14)
check(late_week+timedelta(days=6)<today)
late=req(intern,'POST','/reports/weekly',{'weekStart':late_week.isoformat(),'summary':'Late report '+TAG,'blockers':'','evidenceUrl':None},201)
check(late['weekStart']==late_week.isoformat() and late['status']=='Submitted' and late['submittedAt'] is not None)
def schedule(day,begin='09:00:00',finish='17:00:00',kind='Shift'):
    return {'programId':prid,'date':day.isoformat(),'startTime':begin,'endTime':finish,'title':'Shift '+TAG,'kind':kind}
req(intern,'POST','/attendance/check-out',expected=400)
req(intern,'POST','/attendance/check-in',expected=400)
req(hr,'POST','/schedules',schedule(today,begin='17:00:00',finish='09:00:00'),400)
req(hr,'POST','/schedules',schedule(today+timedelta(days=30)),400)
req(hr,'POST','/schedules',schedule(yesterday),201)
req(hr,'POST','/schedules',schedule(tomorrow),201)
req(hr,'POST','/schedules',schedule(today,kind='Milestone'),201)
begin=max(now-timedelta(minutes=30),datetime.combine(today,datetime.min.time(),now.tzinfo)).strftime('%H:%M:%S')
finish=min(now+timedelta(minutes=30),datetime.combine(today,datetime.max.time(),now.tzinfo)).strftime('%H:%M:%S')
sid=req(hr,'POST','/schedules',schedule(today,begin,finish),201)['id']
req(hr,'POST','/schedules',schedule(today),400)
mine=req(intern,'GET','/interns/me/schedule');check(mine['assignment']['profileId']==pid);check(len(mine['items'])==4)
check(req(other,'GET','/interns/me/schedule')['assignment'] is None);check(req(other,'GET',f'/schedules?programId={prid}')==[])
req(other,'POST','/attendance/check-in',expected=400)
req(hr,'POST','/attendance/check-in',expected=403)
record=req(intern,'POST','/attendance/check-in');check(record['lateMinutes']>0)
req(intern,'POST','/attendance/check-in',expected=409)
req(hr,'PUT',f'/schedules/{sid}',schedule(today,begin,finish),409)
req(hr,'DELETE',f'/schedules/{sid}',expected=409)
record=req(intern,'POST','/attendance/check-out');check(record['earlyMinutes']>0)
req(intern,'POST','/attendance/check-out',expected=409)
check(req(intern,'GET','/attendance/me')['today']['checkOut'] is not None);check(req(other,'GET','/attendance/me')['today'] is None)
req(intern,'GET','/attendance/report',expected=403);req(mentor,'GET','/attendance/report',expected=403)
req(intern,'POST','/leave',{'from':today.isoformat(),'to':today.isoformat(),'reason':'Already checked in'},400)
leave=req(intern,'POST','/leave',{'from':tomorrow.isoformat(),'to':tomorrow.isoformat(),'reason':'School appointment'},201)['id']
req(intern,'POST','/leave',{'from':tomorrow.isoformat(),'to':tomorrow.isoformat(),'reason':'Duplicate'},409)
req(intern,'PATCH',f'/leave/{leave}',{'status':'Approved'},403)
req(hr,'PATCH',f'/leave/{leave}',{'status':'Rejected'},400)
req(hr,'PATCH',f'/leave/{leave}',{'status':'Approved'})
req(hr,'PATCH',f'/leave/{leave}',{'status':'Approved'},409)
check(req(other,'GET','/leave')==[])
report=req(hr,'GET',f'/attendance/report?from={yesterday.isoformat()}&to={tomorrow.isoformat()}&profileId={pid}')
s=report['summary'][0];check((s['workDays'],s['lateDays'],s['earlyDays'],s['leaveDays'],s['absentDays'])==(1,1,1,1,1))
check(len(report['details'])==3)
req(hr,'GET','/attendance/report?from=2026-10-06&to=2026-10-01',expected=400)
req(hr,'GET','/attendance/report?from=2020-01-01&to=2026-10-01',expected=400)
cleanup_program=req(hr,'POST','/programs',dict(program,name='Cleanup '+TAG),201)['id']
cleanup_assignment=req(hr,'POST','/assignments',{'profileId':otherid,'programId':cleanup_program,'mentorId':mentors[1][1]},201)['id']
check(len(req(mentor2,'GET','/assignments'))==1)
req(other,'DELETE',f'/assignments/{cleanup_assignment}',expected=403)
req(hr,'DELETE',f'/assignments/{cleanup_assignment}',expected=204)
check(req(other,'GET','/assignments')==[] and req(mentor2,'GET','/assignments')==[])
req(hr,'DELETE',f'/programs/{cleanup_program}',expected=204)
req(hr,'DELETE',f'/programs/{prid}',expected=409);req(hr,'DELETE',f'/assignments/{assignment}',expected=409)
req(mentor,'POST','/programs',program,403);check(len(req(mentor,'GET','/programs'))==1)
print(f'PASS Sprint 2: {checks} HTTP/assertion checks. Tag {TAG}.')
