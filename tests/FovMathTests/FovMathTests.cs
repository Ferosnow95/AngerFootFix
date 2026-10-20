using System; using AngerFootUltrawide;
class T{ static int fails=0; static void Eq(string n,double a,double b,double t=0.01){ bool ok=Math.Abs(a-b)<t; if(!ok)fails++; Console.WriteLine((ok?"PASS ":"FAIL ")+n+" got "+a.ToString("0.###")+" want "+b.ToString("0.###"));}
static void Main(){
 double A=32.0/9;
 Eq("slider 75 = original",FovMath.WeaponFov(60,75,1,A,false,0),60);
 Eq("strength 0 = original",FovMath.WeaponFov(60,110,0,A,false,0),60);
 Eq("weapon=main when equal",FovMath.WeaponFov(75,110,1,A,false,0),110);
 double w=FovMath.WeaponFov(60,110,1,A,false,0);
 double r1=Math.Tan(w/2*Math.PI/180)/Math.Tan(30*Math.PI/180), r2=Math.Tan(55*Math.PI/180)/Math.Tan(37.5*Math.PI/180);
 Eq("same scale as world",r1,r2,1e-6);
 Eq("offset adds",FovMath.WeaponFov(60,75,1,A,false,5),65);
 Eq("16:9 placement no-op at 16:9",FovMath.WeaponFov(60,75,1,16.0/9,true,0),60);
 double m=FovMath.WeaponFov(60,75,1,A,true,0); Eq("16:9 placement doubles tan at 32:9",Math.Tan(m/2*Math.PI/180),2*Math.Tan(30*Math.PI/180),1e-6);
 Eq("idempotent (computed from original)",FovMath.WeaponFov(60,110,1,A,false,0),w,1e-9);
 Eq("clamp",FovMath.WeaponFov(60,120,2,A,true,60),170);
 Eq("hfov 75v @32:9",FovMath.HorizontalFov(75,A),139.74,0.01);
 Console.WriteLine("example 60 base, slider 110 -> "+w.ToString("0.0"));
 Environment.Exit(fails);}}
