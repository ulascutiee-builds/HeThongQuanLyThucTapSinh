# Original: tests/sprint2_smoke.py, lines 102-102; Sprint 3 Excel rows 56
req(intern,'GET','/attendance/report',expected=403);req(mentor,'GET','/attendance/report',expected=403)

# Original: tests/sprint2_smoke.py, lines 110-115; Sprint 3 Excel rows 56
check(req(other,'GET','/leave')==[])
report=req(hr,'GET',f'/attendance/report?from={yesterday.isoformat()}&to={tomorrow.isoformat()}&profileId={pid}')
s=report['summary'][0];check((s['workDays'],s['lateDays'],s['earlyDays'],s['leaveDays'],s['absentDays'])==(1,1,1,1,1))
check(len(report['details'])==3)
req(hr,'GET','/attendance/report?from=2026-10-06&to=2026-10-01',expected=400)
req(hr,'GET','/attendance/report?from=2020-01-01&to=2026-10-01',expected=400)

