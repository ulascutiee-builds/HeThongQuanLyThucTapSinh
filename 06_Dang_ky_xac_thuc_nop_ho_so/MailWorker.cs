using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
public sealed class MailJob
{
    public int Id{get;set;} public string EventKey{get;set;}="";public string Recipient{get;set;}="";public string Subject{get;set;}="";public string Body{get;set;}="";public bool IsHtml{get;set;}
    public string Status{get;set;}="Queued";public int Attempts{get;set;} public DateTimeOffset DueAt{get;set;}=DateTimeOffset.UtcNow;public DateTimeOffset? SentAt{get;set;} public string? LastError{get;set;}
}
public sealed class MailWorker(IServiceScopeFactory scopes,IConfiguration config,IWebHostEnvironment env,ILogger<MailWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while(!stop.IsCancellationRequested){try{await Process(stop);}catch(Exception e){logger.LogWarning(e,"Email queue unavailable");}try{await Task.Delay(3000,stop);}catch(OperationCanceledException){break;}}
    }
    async Task Process(CancellationToken stop)
    {
        using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<CareerDbContext>();var now=DateTimeOffset.UtcNow;
        var jobs=await db.MailJobs.Where(x=>(x.Status=="Queued"||x.Status=="Processing")&&x.DueAt<=now&&x.Attempts<5).OrderBy(x=>x.Id).Take(10).AsNoTracking().ToListAsync(stop);
        foreach(var job in jobs){
            var claimed=await db.MailJobs.Where(x=>x.Id==job.Id&&x.DueAt<=now&&(x.Status=="Queued"||x.Status=="Processing")).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Processing").SetProperty(x=>x.DueAt,now.AddMinutes(5)).SetProperty(x=>x.Attempts,x=>x.Attempts+1),stop);if(claimed==0)continue;
            try{
                using var smtp=new SmtpClient();var host=config["Email:SmtpHost"];
                if(string.IsNullOrWhiteSpace(host)){var path=Path.Combine(env.ContentRootPath,"App_Data","mail");Directory.CreateDirectory(path);smtp.DeliveryMethod=SmtpDeliveryMethod.SpecifiedPickupDirectory;smtp.PickupDirectoryLocation=path;}
                else{smtp.Host=host;smtp.Port=config.GetValue("Email:Port",587);smtp.EnableSsl=config.GetValue("Email:EnableSsl",true);smtp.Credentials=new NetworkCredential(config["Email:Username"],config["Email:Password"]);smtp.Timeout=15000;}
                using var message=new MailMessage(config["Email:From"]??"noreply@sprint1.local",job.Recipient,job.Subject,job.Body){IsBodyHtml=job.IsHtml};await smtp.SendMailAsync(message,stop);
                await db.MailJobs.Where(x=>x.Id==job.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Sent").SetProperty(x=>x.SentAt,DateTimeOffset.UtcNow).SetProperty(x=>x.LastError,(string?)null),stop);
                logger.LogInformation("Email {Id} sent",job.Id);
            }catch(Exception e){var attempt=job.Attempts+1;await db.MailJobs.Where(x=>x.Id==job.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,attempt>=5?"Failed":"Queued").SetProperty(x=>x.LastError,e.Message).SetProperty(x=>x.DueAt,DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2,attempt)*15)),stop);logger.LogWarning("Email {Id} failed on attempt {Attempt}",job.Id,attempt);}
        }
    }
}
