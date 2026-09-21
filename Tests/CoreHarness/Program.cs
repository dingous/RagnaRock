using System.Text.Json;
using RagnaRock.Core;

// Compiles and executes the actual shared C# rules, without the Unity runtime or NuGet packages.
int assertions=0;
void Check(bool condition,string message)
{
    assertions++;if(!condition)throw new InvalidOperationException("FAIL: "+message);
}
var campaign=JsonSerializer.Deserialize<CampaignDefinition>(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Campaign.json")),
    new JsonSerializerOptions{IncludeFields=true});
campaign.Validate();Check(campaign.TotalWaves==54,"54 waves");
Check(campaign.chapters.Length==18,"18 acts");
int bosses=0;WavePlan previous=new WavePlan(0,campaign);
for(int i=0;i<campaign.TotalWaves;i++)
{
    var wave=new WavePlan(i,campaign);
    Check(wave.Quota>=previous.Quota&&wave.Quota<=132,"quota");
    Check(wave.HealthScale>=previous.HealthScale,"health growth");
    Check(wave.SpawnInterval<=previous.SpawnInterval&&wave.SpawnInterval>=.15f,"spawn interval");
    Check(wave.HasBoss==((i+1)%3==0),"boss schedule");
    if(wave.HasBoss)bosses++;previous=wave;
}
Check(bosses==18,"boss count");
var a=new RunRandom(493); var b=new RunRandom(493);
for(int i=0;i<10000;i++)Check(a.NextUInt()==b.NextUInt(),"determinism");
for(uint seed=1;seed<=100;seed++)
{
    var stats=new BandStats();var random=new RunRandom(seed);int applied=0;
    while(true)
    {
        var draft=UpgradeDraft.Draw(stats,random);
        Check(draft.Distinct().Count()==draft.Length,"unique options");
        foreach(var option in draft)Check(stats.CanUpgrade(option),"eligible option");
        if(draft.Length==0)break;
        Check(stats.Apply(draft[random.Range(0,draft.Length)]),"apply upgrade");applied++;
        Check(stats.Validate(),"valid stats after upgrade");
    }
    Check(applied==68,"28 weapon + 40 equipment upgrades");
}
var xp=new BandStats();Check(xp.AddExperience(55)==2&&xp.level==3&&xp.experience==0,"level rollover");
var data=new RunCheckpoint{seed=11,stats=new BandStats(),health=180};
Check(data.IsValid(54),"valid checkpoint");data.health=float.NaN;Check(!data.IsValid(54),"NaN checkpoint refused");
Check(CombatMath.ApplyArmor(100,99)==25,"armor bounded");
Check(CombatMath.SegmentDistanceSquared(3,4,0,0,0,0)==25,"zero segment");
foreach(BandRole role in Enum.GetValues<BandRole>())for(int rank=2;rank<=8;rank++)
{
    Check(CombatMath.WeaponDamage(role,rank)>CombatMath.WeaponDamage(role,rank-1),"weapon growth");
    Check(CombatMath.WeaponCooldown(role,rank)<CombatMath.WeaponCooldown(role,rank-1),"weapon rate");
}
Console.WriteLine($"PASS: {assertions:N0} assertions against actual RagnaRock.Core C#.");
Console.WriteLine("Does NOT test Unity import, shaders, rendering, audio, input, scene, or a player build.");
