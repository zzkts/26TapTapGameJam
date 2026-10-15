"""Original, deterministic foley-inspired synthesis. No downloaded recordings or external service.
Outputs editable 44.1 kHz PCM16 mono game cues and a sequential audition file.
"""
from pathlib import Path
import hashlib, json, math, wave
import numpy as np

SR = 44100
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Audio/WaterDrain'
DOC = ROOT / 'Docs/AudioPolish'
OUT.mkdir(parents=True, exist_ok=True)
DOC.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(26)

def time(s): return np.arange(round(s * SR)) / SR
def fade(x, a=.002, b=.025):
    x=x.copy(); n=min(round(a*SR),len(x)//2); m=min(round(b*SR),len(x)//2)
    if n: x[:n]*=np.sin(np.linspace(0,np.pi/2,n))**2
    if m: x[-m:]*=np.cos(np.linspace(0,np.pi/2,m))**2
    return x
def bubble(s, f=700, drift=-.45, decay=.045):
    t=time(s); freq=f*(1+drift*(1-np.exp(-t/.025)))
    phase=2*np.pi*np.cumsum(freq)/SR
    return fade((np.sin(phase)+.07*np.sin(phase*2.03))*np.exp(-t/decay),.0008,.012)
def noise(s, low=220, high=6500):
    n=round(s*SR); f=np.fft.rfftfreq(n,1/SR)
    band=(1-np.exp(-(f/max(low,1))**4))*np.exp(-(f/high)**4)
    x=np.fft.irfft(np.fft.rfft(rng.standard_normal(n))*band,n)
    return x/max(np.std(x),1e-8)
def puff(s=.3, low=200, high=4800, decay=.07, rise=.003):
    t=time(s); return fade(noise(s,low,high)*(1-np.exp(-t/rise))*np.exp(-t/decay))
def add(dst, src, start=0, gain=1):
    i=round(start*SR); n=min(len(src),len(dst)-i)
    if n>0: dst[i:i+n]+=src[:n]*gain
def spray(s=.4, count=14):
    x=np.zeros(round(s*SR)); add(x,puff(min(.18,s),600,8500,.025),gain=.16)
    for _ in range(count):
        at=rng.uniform(0,max(.001,s-.09)); length=min(rng.uniform(.04,.12),s-at)
        add(x,bubble(length,rng.uniform(650,2100),rng.uniform(-.55,-.15),.025),at,rng.uniform(.04,.20)*(1-at/s))
    return x
def chimes(s, notes, spacing=.13, decay=.20):
    x=np.zeros(round(s*SR))
    for i,f in enumerate(notes):
        t=time(max(.01,s-i*spacing)); env=(1-np.exp(-t/.004))*np.exp(-t/decay)
        a=(np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*2.008*t)+.05*np.sin(2*np.pi*f*3*t))*env
        add(x,fade(a),i*spacing,.5); add(x,bubble(.11,f*1.15,-.30,.033),i*spacing,.20)
    return x

bank={}
def keep(name, x, db=-24):
    x=np.asarray(x,dtype=float); x-=np.mean(x); x=fade(x,.001,.02)
    active=x[np.abs(x)>max(np.max(np.abs(x))*.015,1e-8)]
    rms=np.sqrt(np.mean(active**2)) if len(active) else 0
    if rms: x*=10**(db/20)/rms
    peak=max(np.max(np.abs(x)),1e-8)
    if peak>.72: x*=.72/peak
    bank[name]=x

for i in range(3):
    rng=np.random.default_rng(260+i)
    x=spray(.26+i*.015,7); add(x,bubble(.20,650+i*70,-.48,.048),gain=.55)
    add(x,puff(.15,300,6000,.028),gain=.16); keep(f'water_shot_{i+1:02}',x,-25)
    x=spray(.42+i*.02,20); add(x,bubble(.24,340+i*25,-.22,.06),gain=.28)
    keep(f'water_impact_{i+1:02}',x,-25)
rng=np.random.default_rng(268)
x=np.zeros(round(.72*SR))
for i in range(9):
    add(x,bubble(.17,220+45*i,.4,.045),i*.055,.25+i*.018)
add(x,puff(.68,150,2200,.32,.16),gain=.13); keep('absorb',x,-24)
x=spray(.58,23);add(x,puff(.40,250,6000,.11),gain=.22);add(x,bubble(.21,410,-.5,.06),gain=.38);keep('release',x,-24)
x=bubble(.24,280,.9,.07)+puff(.24,400,5000,.028)*.05;keep('jump',x,-26)
x=spray(.28,8);add(x,bubble(.21,520,.65,.065),gain=.6);keep('air_step',x,-24)
x=bubble(.20,125,-.38,.034)+puff(.20,70,1900,.028)*.20;keep('land',x,-29)
x=puff(.32,350,6600,.11,.012);add(x,spray(.30,8),.01,.5);keep('dash',x,-25)
x=spray(.31,6);add(x,bubble(.23,350,-.45,.055),gain=.7);keep('seed',x,-25)
x=spray(.48,8)[::-1].copy();add(x,bubble(.18,620,.1,.05),.27,.5);keep('recall',x,-25)
keep('gene',chimes(.85,[659.25,830.61,987.77],.13,.23)+spray(.85,10)*.3,-24)
keep('heal',chimes(.75,[523.25,659.25,783.99],.10,.22),-26)
keep('hurt',bubble(.25,140,-.55,.047)+puff(.25,110,2200,.045)*.3,-24)
keep('death',chimes(.9,[392,329.63,261.63,196],.13,.16)+bubble(.9,160,-.7,.17)*.25,-24)
x=puff(.80,650,8200,.21,.018);add(x,spray(.7,17),gain=.30);add(x,bubble(.12,170,-.2,.03),gain=.23);keep('steam',x,-27)
x=puff(.38,1800,7600,.035)*.18
for i in range(5): add(x,bubble(.12,1500+i*170,-.18,.026),i*.047,.23)
keep('conduction',x,-26)
x=puff(.82,90,1900,.21,.03)*.15;add(x,chimes(.82,[293.66,440,587.33],.12,.18),gain=.5)
for i in range(8): add(x,bubble(.15,260+65*i,.14,.035),i*.07,.12)
keep('growth',x,-27)
x=bubble(.67,88,-.55,.11)+puff(.67,90,4300,.075)*.40;add(x,spray(.55,24),.07,.45);keep('explosion',x,-22)
x=puff(.45,1300,9200,.025)*.2
for i in range(13): add(x,bubble(.08,rng.uniform(900,3000),-.1,.014),rng.uniform(0,.25),.12)
keep('fracture',x,-25)
keep('node',chimes(1.15,[392,587.33,783.99],.15,.28)+spray(1.15,12)*.3,-23)
keep('chapter',chimes(1.2,[293.66,440,587.33],.18,.34),-26)
keep('resonance',chimes(1.3,[523.25,659.25,783.99,1046.5],.12,.29)+spray(1.3,15)*.22,-23)
keep('victory',chimes(2.6,[392,493.88,587.33,783.99,987.77],.20,.55)+spray(2.6,22)*.12,-23)
x=puff(.62,400,5800,.14)*.22;add(x,bubble(.16,160,-.2,.03),gain=.6);add(x,chimes(.4,[880,1174.66],.08,.11),.15,.25);keep('chest',x,-25)
keep('ui',bubble(.11,980,-.25,.018),-29)
keep('fire_shot',puff(.27,300,6500,.046)*.3+bubble(.27,170,-.5,.055),-26)
keep('poison_shot',bubble(.29,230,-.65,.065)+spray(.29,7)*.5,-25)

def write(path, x):
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes((np.clip(x,-1,1)*32767).astype('<i2').tobytes())

report=[]
for name,x in bank.items():
    path=OUT/(name+'.wav');write(path,x)
    report.append({'name':name,'seconds':round(len(x)/SR,3),'sampleRate':SR,'peakDBFS':round(20*np.log10(max(np.max(np.abs(x)),1e-9)),2),'rmsDBFS':round(20*np.log10(max(np.sqrt(np.mean(x*x)),1e-9)),2),'firstSample':int(x[0]*32767),'lastSample':int(x[-1]*32767),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
preview_names=['water_shot_01','water_shot_02','water_shot_03','water_impact_01','absorb','release','air_step','dash','seed','steam','conduction','growth','explosion','gene','node','resonance','victory']
preview=np.concatenate([np.concatenate([bank[n],np.zeros(round(.38*SR))]) for n in preview_names])
write(DOC/'WaterDrain-SFX-Preview.wav',preview)
(DOC/'Synthesis.json').write_text(json.dumps({'method':'Original offline synthesis, no source recordings or paid service','sampleRate':SR,'clips':report,'previewOrder':preview_names},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'clips':len(bank),'seconds':round(sum(len(x) for x in bank.values())/SR,2),'previewSeconds':round(len(preview)/SR,2),'maxPeak':max(r['peakDBFS'] for r in report)},ensure_ascii=False))
