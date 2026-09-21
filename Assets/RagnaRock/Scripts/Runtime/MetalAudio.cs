using System;
using System.Collections;
using RagnaRock.Core;
using UnityEngine;

namespace RagnaRock
{
    /// <summary>Original synthesized score. No samples, lyrics, melody transcriptions, or recordings of real bands.</summary>
    public sealed class MetalAudio : MonoBehaviour
    {
        private readonly AudioSource[] music=new AudioSource[2];
        private readonly AudioSource[] voices=new AudioSource[10];
        private readonly AudioClip[] sounds=new AudioClip[8];
        private readonly float[] shotGate=new float[4];
        private GameOptions options;
        private Coroutine generation;
        private int activeSource,currentChapter=-1,voiceIndex;
        private float crossfade=1f;
        private bool paused;
        private const int SampleRate=22050;
        public void Initialize(GameOptions settings)
        {
            options=settings;
            for(int i=0;i<music.Length;i++)
            {
                music[i]=gameObject.AddComponent<AudioSource>();music[i].loop=true;music[i].playOnAwake=false;
                music[i].spatialBlend=0;music[i].priority=160;
            }
            for(int i=0;i<voices.Length;i++)
            {
                voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;
                voices[i].spatialBlend=0;voices[i].priority=100;
            }
            for(int i=0;i<sounds.Length;i++)sounds[i]=CreateSound(i);
        }
        public void SetChapter(ChapterDefinition chapter,int index)
        {
            if(index==currentChapter)return;currentChapter=index;
            if(generation!=null)StopCoroutine(generation);
            generation=StartCoroutine(Compose(chapter.bpm,index));
        }
        public void SetPaused(bool value)
        {
            paused=value;
            foreach(var source in music)
                if(source!=null){if(value)source.Pause();else source.UnPause();}
            if(value)foreach(var source in voices)if(source!=null)source.Stop();
        }
        public void Shot(BandRole role,int rank)
        {
            int i=(int)role;if(Time.unscaledTime<shotGate[i])return;
            shotGate[i]=Time.unscaledTime+.11f;
            Sound(i,role==BandRole.Guitar?.10f:.17f,1f+(rank-1)*.012f);
        }
        public void Impact(){Sound(4,.14f,.96f);}
        public void Upgrade(){Sound(5,.35f,1f);}
        public void Super(){Sound(6,.42f,1f);}
        public void Click(){Sound(7,.25f,1f);}
        private void Sound(int id,float gain,float pitch)
        {
            if(options==null||options.mute||options.effects<=0)return;
            var source=voices[voiceIndex++%voices.Length];source.Stop();source.clip=sounds[id];source.pitch=pitch;
            source.volume=gain*options.effects*options.master;source.Play();
        }
        private void Update()
        {
            if(options==null)return;
            if(!paused)crossfade=Mathf.Min(1f,crossfade+Time.unscaledDeltaTime*.55f);
            float gain=options.mute?0:options.master*options.music;
            for(int i=0;i<2;i++)
            {
                if(music[i]==null)continue;
                music[i].volume=gain*(i==activeSource?crossfade:1-crossfade);
                if(i!=activeSource&&crossfade>=1&&music[i].isPlaying)music[i].Stop();
            }
        }
        private IEnumerator Compose(int bpm,int chapter)
        {
            const int steps=64;
            double stepSeconds=60.0/bpm/4.0;
            int count=(int)(steps*stepSeconds*SampleRate);
            var data=new float[count];var random=new System.Random(9011+chapter*101);
            // Generated root/interval patterns; deliberately not a transcription of any referenced song.
            int[] intervals={0,0,3,0,6,5,1,0,0,7,3,5,0,1,6,0};
            double tuning=82.406889*Math.Pow(2,-Math.Min(7,chapter*.45)/12.0);
            bool fast=chapter>=10,melodic=chapter==6||chapter==7||chapter==8||chapter==15;
            bool slow=chapter==2||chapter==11;
            for(int i=0;i<count;i++)
            {
                double time=i/(double)SampleRate;
                int step=(int)(time/stepSeconds);double local=time-step*stepSeconds;
                int interval=intervals[(step+(chapter*3))%intervals.Length];
                if(step%8<3)interval=0;
                double frequency=tuning*Math.Pow(2,interval/12.0);
                bool rest=(step+chapter)%13==12;
                double gate=Math.Exp(-local*(slow?8:18))*Math.Min(1,local*450);
                double g=Math.Sin(2*Math.PI*frequency*time)+.46*Math.Sin(2*Math.PI*frequency*1.5*time)
                    +.32*Math.Sin(2*Math.PI*frequency*2.004*time)+.18*Math.Sin(2*Math.PI*frequency*3*time);
                double guitar=Math.Tanh(g*3.3)*gate*(rest?.07:.33);
                double bass=Math.Sin(2*Math.PI*frequency*.5*time)*Math.Exp(-local*7)*.22;
                double drum=0;
                bool kick=step%8==0||step%8==3||(fast&&step%2==0)||(chapter==11&&step%8==5);
                if(kick&&local<.14)
                    drum+=Math.Sin(2*Math.PI*(48*local+12*(1-Math.Exp(-local*25))))*Math.Exp(-local*26)*.65;
                bool snare=fast?step%4==2:step%8==4;
                double noise=random.NextDouble()*2-1;
                if(snare&&local<.16)
                    drum+=(noise*.7+Math.Sin(2*Math.PI*175*local)*.25)*Math.Exp(-local*32)*.48;
                bool hat=fast||step%2==0;
                if(hat&&local<.055)drum+=noise*Math.Exp(-local*92)*.11;
                double lead=0;
                if(melodic)
                {
                    int note=intervals[(step/2+chapter+5)%intervals.Length]+12;
                    double f=tuning*Math.Pow(2,note/12.0);
                    lead=(Math.Sin(2*Math.PI*f*time)+.18*Math.Sin(2*Math.PI*f*2*time))*.085*Math.Exp(-local*6);
                }
                double edge=Math.Min(1,Math.Min(i/(SampleRate*.015),(count-1-i)/(SampleRate*.025)));
                data[i]=(float)(Math.Tanh((guitar+bass+drum+lead)*1.1)*.68*edge);
                if(i>0&&i%8192==0)yield return null;
            }
            var clip=AudioClip.Create("RagnaRock original • act "+(chapter+1),count,1,SampleRate,false);
            clip.SetData(data,0);
            int next=1-activeSource;music[next].Stop();
            if(music[next].clip!=null)Destroy(music[next].clip);
            music[next].clip=clip;music[next].volume=0;music[next].Play();
            if(paused)music[next].Pause();
            activeSource=next;crossfade=0;generation=null;
        }
        private static AudioClip CreateSound(int kind)
        {
            float duration=kind==5?.5f:kind==6?.72f:kind==7?.07f:.19f;
            int count=(int)(duration*SampleRate);float[] samples=new float[count];var rng=new System.Random(729+kind);
            for(int i=0;i<count;i++)
            {
                double t=i/(double)SampleRate,noise=rng.NextDouble()*2-1;
                double frequency=kind==0?180:kind==1?460:kind==2?62:kind==3?115:kind==4?75:kind==5?660:kind==6?48:820;
                if(kind==5)frequency*=1+Math.Floor(t*10)*.2;
                double signal=Math.Sin(2*Math.PI*frequency*t)*.7;
                if(kind==3||kind==4||kind==6)signal=signal*.65+noise*.45;
                double decay=Math.Exp(-t*(kind==6?5:kind==5?6:kind==2?17:28));
                double attack=Math.Min(1,t*400),tail=Math.Min(1,(duration-t)*200);
                samples[i]=(float)(Math.Tanh(signal*1.8)*decay*attack*tail*.7);
            }
            var clip=AudioClip.Create("RagnaRock original SFX "+kind,count,1,SampleRate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDestroy()
        {
            StopAllCoroutines();
            foreach(var clip in sounds)if(clip!=null)Destroy(clip);
            foreach(var source in music)if(source!=null){source.Stop();if(source.clip!=null)Destroy(source.clip);}
        }
    }
}
