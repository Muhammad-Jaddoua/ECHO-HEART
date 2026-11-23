"""Deterministic original arrangements and sound design; no samples or existing melodies used."""
from pathlib import Path
import argparse,hashlib,json,math,subprocess,tempfile,wave
import numpy as np
from scipy.signal import butter,sosfilt

ROOT=Path(__file__).resolve().parents[3];RATE=22050
p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);args=p.parse_args()
MUSIC=ROOT/'Assets/_Project/Audio/Music/Jehad/V1_5';SFX=ROOT/'Assets/_Project/Audio/SFX/Jehad/V1_5'
MUSIC.mkdir(parents=True,exist_ok=True);SFX.mkdir(parents=True,exist_ok=True)
records=[]
def hz(note):return 440*2**((note-69)/12)
def voice(note,duration,kind):
    t=np.arange(int(duration*RATE))/RATE;f=hz(note)
    if kind=='bell':
        s=np.sin(2*np.pi*f*t)*np.exp(-3*t)+.28*np.sin(2*np.pi*f*2.003*t)*np.exp(-6*t)+.12*np.sin(2*np.pi*f*3.99*t)*np.exp(-9*t)
    elif kind=='plucked':s=(np.sin(2*np.pi*f*t)+.25*np.sin(4*np.pi*f*t)+.1*np.sin(6*np.pi*f*t))*np.exp(-5*t)
    elif kind=='flute':s=(np.sin(2*np.pi*f*t+.016*np.sin(2*np.pi*5*t))+.12*np.sin(4*np.pi*f*t))*np.minimum(t/.09,1)*np.minimum((duration-t)/.16,1)
    elif kind=='pad':s=(np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*1.002*t)+.1*np.sin(4*np.pi*f*t))*np.minimum(t/.22,1)*np.minimum((duration-t)/.35,1)
    else:s=np.sin(2*np.pi*f*t)*np.exp(-2*t)
    fade=max(1,int(RATE*.008));s[:fade]*=np.linspace(0,1,fade);s[-fade:]*=np.linspace(1,0,fade)
    return s
def add(buffer,sound,at,gain=.1,pan=0):
    start=int(at*RATE)%len(buffer);ix=(np.arange(len(sound))+start)%len(buffer)
    if buffer.ndim==1:np.add.at(buffer,ix,sound*gain)
    else:
        np.add.at(buffer[:,0],ix,sound*gain*math.sqrt((1-pan)/2));np.add.at(buffer[:,1],ix,sound*gain*math.sqrt((1+pan)/2))
def save(path,data,loop=False):
    data=np.nan_to_num(data);peak=float(np.max(np.abs(data)));data=data/max(1,peak/.66)
    pcm=np.rint(np.clip(data,-1,1)*32767).astype('<i2');channels=1 if data.ndim==1 else data.shape[1]
    if path.suffix=='.ogg':
        with tempfile.TemporaryDirectory() as temp:
            wav=Path(temp)/'audio.wav'
            with wave.open(str(wav),'wb') as out:out.setparams((channels,2,RATE,len(data),'NONE','not compressed'));out.writeframes(pcm.tobytes())
            subprocess.run(['ffmpeg','-v','error','-y','-i',str(wav),'-c:a','libvorbis','-q:a','4','-map_metadata','-1',str(path)],check=True)
    else:
        with wave.open(str(path),'wb') as out:out.setparams((channels,2,RATE,len(data),'NONE','not compressed'));out.writeframes(pcm.tobytes())
    records.append({'path':path.relative_to(ROOT).as_posix(),'duration_seconds':len(data)/RATE,'channels':channels,'sample_rate':RATE,'loop':loop,'pcm_peak':float(np.max(np.abs(data))),'pcm_rms':float(np.sqrt(np.mean(data**2))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size})

chords=[(50,54,57,61),(47,50,54,57),(43,47,50,57),(45,49,52,59)]
melody=[(0,74),(1.5,78),(3,81),(4.5,83),(6,78),(7,76),(8.5,74),(10,71),(11.5,73),(13,76),(14.5,78),(16,81),(18,78),(19.5,76),(21,74),(23,69),(25,71),(27,73),(29,76),(31,74),(34,78),(36,81),(38.5,83),(41,81),(43,78),(45,76)]
for index,name in enumerate(['Meadow','Amber','Lake','Pine','Alpine','Guardian']):
    beat=60/(108 if name=='Guardian' else 90);duration=beat*48;mix=np.zeros((int(duration*RATE),2))
    for bar in range(12):
        chord=chords[bar%4]
        for j,note in enumerate(chord):add(mix,voice(note,beat*4.4,'pad'),bar*4*beat,.037,(-.5,.2,-.2,.5)[j])
        for b in [0,2]:add(mix,voice(chord[0]-12,beat*1.5,'bass'),(bar*4+b)*beat,.105)
        pattern=[0,2,1,3,2,1,3,2]
        for tick in range(8):
            if name=='Alpine' and tick%2:continue
            note=chord[pattern[tick]]+12+(12 if name=='Lake' else 0)
            kind='bell' if name in ['Lake','Alpine'] else 'plucked'
            add(mix,voice(note,1.7,kind),(bar*4+tick*.5)*beat,.04 if name=='Alpine' else .06,math.sin(tick*1.7)*.55)
        if name=='Guardian':
            for b in range(4):
                t=np.arange(int(.23*RATE))/RATE;drum=np.sin(2*np.pi*(52*t+40*.035*(1-np.exp(-t/.035))))*np.exp(-24*t)
                add(mix,drum,(bar*4+b)*beat,.2 if b in [0,2] else .11)
    for at,note in melody:
        if name=='Alpine' and int(at)%3:continue
        offset={'Meadow':0,'Amber':-12,'Lake':0,'Pine':-12,'Alpine':0,'Guardian':-12}[name]
        kind='flute' if name=='Amber' else 'pad' if name=='Pine' else 'bell' if name in ['Lake','Alpine'] else 'plucked'
        add(mix,voice(note+offset,beat*1.25,kind),at*beat,.09 if name!='Guardian' else .065,-.15)
    # Circular delay makes the reverb tail wrap into the start instead of leaving a loop seam.
    dry=mix.copy()
    for delay,gain in [(.31,.12),(.53,.07),(.83,.04)]:mix+=np.roll(dry,int(RATE*delay),axis=0)*gain
    save(MUSIC/(name+'.ogg'),mix,True)

rng=np.random.default_rng(40215)
for name in ['Water','Forest','AlpineAir']:
    seconds=8;count=RATE*seconds;t=np.arange(count)/RATE
    # Circular FFT filtering avoids a transient at the loop boundary.
    frequencies=np.fft.rfftfreq(count,1/RATE);spectrum=np.fft.rfft(rng.normal(0,1,count))
    center=1600 if name=='Water' else 900 if name=='Forest' else 450
    filt=(1-np.exp(-(frequencies/110)**2))*np.exp(-(frequencies/center)**2)
    ambient=np.fft.irfft(spectrum*filt,n=count);ambient/=max(1,np.max(np.abs(ambient)))
    ambient*=.13*(.8+.2*np.sin(2*np.pi*t/8))
    if name=='Forest':
        for at,n in [(1.1,90),(1.32,94),(4.4,92),(4.59,95),(6.6,88)]:add(ambient,voice(n,.13,'flute'),at,.07)
    save(MUSIC/('Ambience_'+name+'.ogg'),ambient,True)

names=['Relic','Chest','Secret','Capture','Hit','Defeat','PlayerHurt','Death','Restore','EnemyWarning','SlamWarning','RangedWarning','Slam','RangedShot','Power','StepGrass','StepStone','StepWater','Jump','Land','Sword','Bow','Fire']
for i,name in enumerate(names):
    duration=1.2 if name in ['Restore','Secret','Death'] else .65 if name in ['Chest','Capture','Defeat','SlamWarning'] else .3
    t=np.arange(int(duration*RATE))/RATE;noise=rng.normal(0,1,len(t));noise=sosfilt(butter(2,4000,fs=RATE,output='sos'),noise)
    if name in ['Relic','Chest','Secret','Capture','Restore','Power']:
        sound=np.zeros(len(t));notes={'Relic':[81,86,90],'Chest':[62,69,74,78],'Secret':[74,81,86],'Capture':[69,74,81],'Restore':[62,66,69,74,78],'Power':[74,81]}[name]
        for j,n in enumerate(notes):add(sound,voice(n,min(.35,duration*.6),'bell'),j*duration/(len(notes)+2),.18)
    elif name in ['Death','Defeat','PlayerHurt','Hit']:
        f={'Death':220,'Defeat':330,'PlayerHurt':180,'Hit':360}[name]
        sound=(np.sin(2*np.pi*(f*t-f*.32*t*t/max(duration,.1)))*.24+noise*.12)*np.exp(-t*(5/duration))
    elif name in ['SlamWarning','RangedWarning','EnemyWarning']:
        f=95 if name=='SlamWarning' else 480 if name=='RangedWarning' else 270
        sound=(np.sin(2*np.pi*(f*t+f*t*t/duration*.25))+.25*np.sin(4*np.pi*f*t))*.18*np.sin(np.pi*t/duration)**2
    elif name in ['StepGrass','StepStone','StepWater','Land','Slam']:
        cutoff=800 if name=='StepGrass' else 2200 if name=='StepStone' else 3500
        sound=sosfilt(butter(2,cutoff,fs=RATE,output='sos'),noise)*np.exp(-t*(35 if name.startswith('Step') else 13))*.28
        if name in ['Land','Slam']:sound+=np.sin(2*np.pi*(68*t+35*.03*(1-np.exp(-t/.03))))*np.exp(-t*18)*.3
    else:
        f={'Jump':350,'Sword':900,'Bow':1400,'Fire':250,'RangedShot':650}[name]
        sweep=np.sin(2*np.pi*(f*t+(-.7 if name=='Sword' else .6)*f*t*t/duration))
        sound=(noise*.18+sweep*.12)*np.sin(np.pi*t/duration)**2*np.exp(-t*7)
    fade=int(.006*RATE);sound[:fade]*=np.linspace(0,1,fade);sound[-fade:]*=np.linspace(1,0,fade)
    save(SFX/(name+'.wav'),sound)
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps({'provenance':'Original deterministic synthesis and arrangements; no external recordings, samples, or existing melodies.','seed':40215,'assets':records},indent=2)+'\n')
print(json.dumps({'music_tracks':6,'ambience_loops':3,'sfx':len(names),'total_bytes':sum(r['bytes'] for r in records),'report':str(args.report)}))
