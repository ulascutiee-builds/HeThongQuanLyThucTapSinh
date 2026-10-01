import http.cookiejar, json, urllib.request, urllib.error
from pathlib import Path

base = 'http://localhost:5148'
http = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
checks = []
def call(method, path, data=None, expected=200):
    request=urllib.request.Request(base+path, method=method, data=None if data is None else json.dumps(data).encode(), headers={'Content-Type':'application/json'})
    try:
        with http.open(request, timeout=90) as r: status=r.status; body=r.read()
    except urllib.error.HTTPError as e: status=e.code; body=e.read()
    assert status == expected, f'{path}: {status} {body[:1200]}'
    checks.append(f'{method} {path}: {status}')
    return json.loads(body) if body else None

call('POST','/api/auth/login',{'identity':'admin@test.local','password':'LocalTesting-2026!'})
program=call('GET','/api/work/programs')[0]
changed={**program,'title':'Chương trình đã cập nhật'}
call('PUT',f"/api/work/programs/{program['id']}",changed)
call('PUT',f"/api/work/programs/{program['id']}",changed,409)
call('DELETE',f"/api/work/programs/{program['id']}",expected=400)
tasks=call('GET','/api/work/tasks')
assert tasks[0]['progress']==100
reports=call('GET','/api/work/reports')
assert reports[0]['fileName']=='proof.pdf'
backup=call('POST','/api/admin/backups')
result=call('POST','/api/admin/backups/'+backup['file']+'/restore')
assert result['database'].startswith('CareerPortalRestore_')
result['checks']=checks
Path(__file__).with_name('final-result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
