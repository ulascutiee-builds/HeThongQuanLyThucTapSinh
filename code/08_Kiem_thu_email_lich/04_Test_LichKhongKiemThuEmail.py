# Original: tests/sprint2_smoke.py, lines 78-92; Sprint 3 Excel rows 61
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

