using System;
using System.Collections.Generic;
using Unity.Netcode;

// Actual installed NGO interpolation, scalar channel. Controlled clock/packet
// schedule, not a measured transport session or a Unity Rigidbody simulation.
class Program
{
    const double Tick=1d/30,Step=.02,Shot=1;
    record Packet(double Arrival,double Stamp,float Position);
    record Metrics(double Received,double Visible,double Ninety,double Settled);
    static Metrics Run(bool ownerHost,bool observerHost,double oneWay,double jitter,int mode,bool smoothing,bool recoil)
    {
        double senderLag=ownerHost?0:.05+oneWay, receiverLag=observerHost?0:.05+oneWay;
        var packets=new List<Packet>();
        for(int tick=1;tick<=120;tick++)
        {
            double time=tick*Tick;
            double elapsed=Math.Clamp(time-Shot,0,1d/12);
            float value=recoil?(float)(-elapsed+6*elapsed*elapsed):(float)Math.Clamp((time-1)*2,0,2);
            double networkStamp=Math.Floor((time-senderLag)*30+1e-6)*Tick;
            double noise=jitter*Math.Sin(tick*2.3);
            // Only send changed positions, as the production NetworkTransform does.
            if(packets.Count>0 && Math.Abs(value-packets[^1].Position)<.001) continue;
            double deliveryDelay=!ownerHost&&!observerHost?oneWay*2:oneWay;
            packets.Add(new(time+deliveryDelay+noise,networkStamp,value));
        }
        // Reliable delivery preserves order even under injected jitter.
        for(int i=1;i<packets.Count;i++) if(packets[i].Arrival<packets[i-1].Arrival) packets[i]=packets[i] with {Arrival=packets[i-1].Arrival};
        var interp=new BufferedLinearInterpolatorFloat {MaximumInterpolationTime=.1f,LerpSmoothEnabled=smoothing};
        interp.ResetTo(0,-1);
        double received=double.NaN,visible=double.NaN,ninety=double.NaN,settled=double.NaN;
        int next=0;float previous=0,lastRendered=0;double latencyTicks=observerHost?1:Math.Max(2,Math.Round((oneWay*2+Step)/Tick));
        if(!observerHost&&!ownerHost)latencyTicks++;
        for(double time=0;time<4;time+=Step)
        {
            while(next<packets.Count && packets[next].Arrival<=time+1e-7)
            {
                var packet=packets[next++];interp.AddMeasurement(packet.Position,packet.Stamp);
                if(packet.Position<-.001f && double.IsNaN(received))received=time;
            }
            double current=time-receiverLag;
            float position=mode==0?interp.Update((float)Step,current-(observerHost?1:2)*Tick,current):
                interp.Update((float)Step,current-latencyTicks*Tick,Tick,latencyTicks*Tick,true);
            // One fixed-step render delay models the saved Rigidbody Interpolate.
            float rendered=previous;previous=position;
            if(!float.IsFinite(rendered) || (recoil && (rendered>lastRendered+.00001f || rendered<(-1f/24-.00001f))) ||
                (!recoil && (rendered<lastRendered-.00001f || rendered>2.00001f)))
                throw new Exception("Interpolation must remain finite, bounded and monotonic for this monotonic source trajectory.");
            lastRendered=rendered;
            if(recoil)
            {
                if(rendered<-.002f && double.IsNaN(visible))visible=time;
                if(rendered<=-.9f/24 && double.IsNaN(ninety))ninety=time;
            }
            else if(time>=2 && Math.Abs(rendered-2)<.005f && double.IsNaN(settled))settled=time;
        }
        return new(received,visible,ninety,settled);
    }
    static void Main()
    {
        Console.WriteLine("Actual NGO 2.13.3 interpolator; synthetic clocks/packets; 30 Hz network, 50 Hz physics, RB presentation model=20ms.");
        foreach(var path in new[]{(true,false,"Host->Client"),(false,true,"Client->Host"),(false,false,"Client->Client")})
        foreach(var link in new[]{(0d,0d),(.05d,.01d),(.1d,.02d)})
        {
        var beforeRecoil=Run(path.Item1,path.Item2,link.Item1,link.Item2,1,true,true);
        var afterRecoil=Run(path.Item1,path.Item2,link.Item1,link.Item2,0,false,true);
        var beforeStop=Run(path.Item1,path.Item2,link.Item1,link.Item2,1,true,false);
        var afterStop=Run(path.Item1,path.Item2,link.Item1,link.Item2,0,false,false);
        if(!double.IsFinite(afterRecoil.Visible)||!double.IsFinite(afterRecoil.Ninety)||!double.IsFinite(afterStop.Settled) ||
            afterRecoil.Visible>beforeRecoil.Visible+.00001 || afterStop.Settled>beforeStop.Settled+.00001)
            throw new Exception("Candidate must reach movement/stop within baseline latency for the same controlled delivery schedule.");
        foreach(var config in new[]{(1,true,"Lerp+smooth (before)"),(1,false,"Lerp only"),(0,true,"Legacy+smooth"),(0,false,"Legacy only")})
        {
            var recoil=Run(path.Item1,path.Item2,link.Item1,link.Item2,config.Item1,config.Item2,true);
            var stop=Run(path.Item1,path.Item2,link.Item1,link.Item2,config.Item1,config.Item2,false);
            Console.WriteLine($"{path.Item3} oneWay={link.Item1*1000:F0} jitter=+/-{link.Item2*1000:F0} {config.Item3}: firstState={(recoil.Received-Shot)*1000:F0}ms visible={(recoil.Visible-Shot)*1000:F0}ms recoil90={(recoil.Ninety-Shot)*1000:F0}ms stop={(stop.Settled-2)*1000:F0}ms");
        }
        }
        Console.WriteLine("PASS: nine controlled before/after path/latency cases; unchanged delivery schedule and clocks.");
    }
}
