# EXCERPT (not a standalone test module)
# Original path: tests/sprint2_smoke.py
# Original lines: [(58, 66)]

mentors=[]
for suffix in ['a','b']:
    address=TAG+suffix+'mentor@example.test'
    result=req(admin,'POST','/accounts',{'name':'Mentor '+suffix+TAG,'email':address,'roles':['Mentor'],'isActive':True},201)
    uid=result['account']['id'];token=activation(uid,address)
    req(anon,'POST','/auth/activate',{'accountId':uid,'token':token,'password':'TestMentor123!'})
    if suffix=='a':req(mentor,'POST','/auth/login',{'identity':address,'password':'TestMentor123!'})
    mid=req(hr,'POST','/mentors',{'userId':uid,'departmentId':dep,'capacity':1},201)['id'];mentors.append((uid,mid))
req(hr,'POST','/mentors',{'userId':mentors[0][0],'departmentId':dep,'capacity':1},409)
