# Mockup renderer mirroring the SPXWebcastOverlay HUD (1080p units). Usage: python3 mock.py <reference.jpg>
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, numpy as np, sys, os
S=3; W,H=1920,1080
FR='fontsrc/ddin/D-DINExp.ttf'; FB='fontsrc/ddin/D-DIN-Bold.ttf'
def capratio(fp):
    f=ImageFont.truetype(fp,1000); b=f.getbbox('H'); return (b[3]-b[1])/1000
CR={FR:capratio(FR),FB:capratio(FB)}
ev=[('STARTUP',-3),('LIFTOFF',0),('MAX-Q',72),('MECO',145),('STAGE SEP',149),('BOOSTBACK',160),('ENTRY BURN',390),('LANDING BURN',480)]
def frame(t,spd,alt,g,eng,fuel,span,ry,name='STARLINK',events=None):
    evs=events or ev
    bg=Image.new('RGB',(W,H)); d=ImageDraw.Draw(bg)
    for y in range(H): d.line([(0,y),(W,y)],fill=(100+y*25//H,108+y*22//H,120+y*18//H))
    if ry is not None: d.rounded_rectangle([950,ry,972,ry+300],8,fill=(232,232,232)); d.rectangle([950,ry+120,972,ry+150],fill=(40,40,40))
    a=np.array(bg).astype(float); Y,X=np.mgrid[0:H,0:W].astype(float)
    v=np.clip(1-(H-Y)/170,0,1); uu=np.clip((np.abs(X-W/2)-300)/600,0,1); bell=1-uu*uu*(3-2*uu)
    al=((0.36+0.49*bell)*v)[:,:,None]
    base=Image.fromarray((a*(1-al)).astype('uint8')).convert('RGBA')
    # end bands
    yt,yb,xp,xs,xf=H-181,H-82,370,490,570
    Yb,Xb=np.mgrid[0:H,0:W].astype(float); band=np.zeros((H,W))
    for side in (0,1):
        xx=Xb if side==0 else W-Xb
        top=np.where(xx<=xp,yt,np.where(xx<=xs,yt+(xx-xp)/(xs-xp)*(yb-yt),yb))
        av=np.where(xx<=xp,.38,np.where(xx<=xs,.38-(xx-xp)/(xs-xp)*.23,np.clip(.15*(1-(xx-xs)/(xf-xs)),0,1)))
        band=np.maximum(band,av*np.clip(Yb-top+0.5,0,1)*(xx<=xf))
    bi=np.zeros((H,W,4),np.uint8); bi[...,:3]=(5,6,9); bi[...,3]=(band*255).astype(np.uint8)
    base=Image.alpha_composite(base,Image.fromarray(bi,'RGBA'))
    ov=Image.new('RGBA',(W*S,H*S),(0,0,0,0)); d=ImageDraw.Draw(ov,'RGBA')
    P=lambda q:q*S
    def fnt(cap,bold): fp=FB if bold else FR; return ImageFont.truetype(fp,max(1,int(round(cap/CR[fp]*S))))
    def tw(s,cap,sp,bold=True): f=fnt(cap,bold); return (sum(f.getlength(c) for c in s)+sp*S*(len(s)-1))/S
    def txt(cx,cy,s,cap,sp,alpha,bold=True):   # cy = vertical centre of capitals
        f=fnt(cap,bold); x=P(cx)-tw(s,cap,sp,bold)*S/2
        for c in s:
            d.text((x,P(cy+cap/2)),c,font=f,fill=(255,255,255,int(255*alpha)),anchor='ls'); x+=f.getlength(c)+sp*S
    def disc(cx,cy,r,col): d.ellipse([P(cx-r),P(cy-r),P(cx+r),P(cy+r)],fill=col)
    def ring(cx,cy,r,w,col): d.ellipse([P(cx-r),P(cy-r),P(cx+r),P(cy+r)],outline=col,width=max(1,int(w*S)))
    def arc(cx,cy,r,w,f,col): d.arc([P(cx-r),P(cy-r),P(cx+r),P(cy+r)],135,135+270*f,fill=col,width=max(1,int(w*S)))
    gy=H-104;R=67;ly=H-147
    def gauge(cx,f):
        disc(cx,gy,R+5,(0,0,0,89)); arc(cx,gy,R,4,1,(255,255,255,64)); ring(cx,gy,R*.85,1,(255,255,255,64))
        if f>0.002: arc(cx,gy,R,4,min(f,1),(255,255,255,255))
    def gt(cx,l,v,u): txt(cx,gy-30,l,10.5,1,.8); txt(cx,gy+1,v,27,0,1,False); txt(cx,gy+30,u,9.5,1,.75)
    for cx,l,v,u,f in [(140,'SPEED',str(spd),'KM/H',spd/8000),(300,'ALTITUDE',f'{alt:.1f}','KM',alt/100),(W-300,'G-FORCE',f'{g:.1f}','G',g/5)]: gauge(cx,f); gt(cx,l,v,u)
    cx=W-140; gauge(cx,fuel); engines(d,P,cx,gy,eng)
    txt(cx-R-14,gy+R-6,str(eng),10,0,.85)
    x0,x1=W/2-460,W/2+460;A,B=t-span/2,t+span/2;Xf=lambda q:x0+(q-A)/(B-A)*(x1-x0);xm=W/2
    d.rectangle([P(x0),P(ly-0.6),P(xm),P(ly+0.6)],fill=(255,255,255,217)); d.rectangle([P(xm),P(ly-0.5),P(x1),P(ly+0.5)],fill=(255,255,255,71))
    lb=la=-1e9; ib=ia=None; vis=[]
    for n,q in sorted(evs,key=lambda e:e[1]):
        f=(q-A)/(B-A)
        if not 0<=f<=1: continue
        al_=min(1,min(f,1-f)/0.06); x=Xf(q)
        d.rectangle([P(x-0.5),P(ly-7),P(x+0.5),P(ly-4.5)],fill=(255,255,255,int(89*al_)))
        if t>=q: disc(x,ly,3.6,(255,255,255,int(255*al_)))
        else: disc(x,ly,3.6,(0,0,0,int(150*al_))); ring(x,ly,3.6,1.1,(255,255,255,int(255*al_)))
        e=[n,q,x,al_,0]; vis.append(e); h=tw(n,11,1)/2+8
        if x-h>=lb: e[4]=1; lb=x+h; ib=e
        elif x-h>=la: e[4]=-1; la=x+h; ia=e
        elif ib and t>=ib[1]: ib[4]=0; e[4]=1; lb=x+h; ib=e
        elif ia and t>=ia[1]: ia[4]=0; e[4]=-1; la=x+h; ia=e
    for n,q,x,al_,row in vis:
        if row: txt(x,ly+18*row,n,11,1,.85*al_)
    disc(xm,ly,5,(255,255,255,255))
    dg='00:%02d:%02d'%(abs(t)//60,abs(t)%60); pre='T-' if t<0 else 'T+'
    dw=tw(dg,34.5,0,False); pw=tw(pre,22,0); c0=W/2-(dw+pw+12)/2
    txt(c0+pw/2,H-93,pre,22,0,.7); txt(c0+pw+12+dw/2,H-93,dg,34.5,0,1,False); txt(W/2,H-57,name,11,1.5,.75)
    gl=Image.new('RGBA',ov.size,(0,0,0,0)); gd=ImageDraw.Draw(gl); gd.ellipse([P(xm-8),P(ly-8),P(xm+8),P(ly+8)],fill=(255,255,255,90))
    gl=gl.filter(ImageFilter.GaussianBlur(4*S))
    ov=Image.alpha_composite(gl,ov).resize((W,H),Image.LANCZOS)
    return Image.alpha_composite(base,ov).convert('RGB')
def engines(d,P,cx,cy,lit,k=1.0):
    rr=34.3*k; dr=10.25*k
    for i in range(-1,8):
        a=math.radians(22.5+45*i); x=cx if i<0 else cx+math.sin(a)*rr; y=cy if i<0 else cy-math.cos(a)*rr
        on= lit>=1 if i<0 else (lit>=9 or (lit==3 and i in (1,5)))
        d.ellipse([P(x-dr),P(y-dr),P(x+dr),P(y+dr)],fill=(255,255,255,255 if on else 69))
if __name__=='__main__':
    frame(-20,0,0.1,1.0,0,1.0,640,560).save('mockup_t-20.png')
    frame(-3,0,0.1,1.0,9,1.0,600,560).save('mockup_t-3.png')
    frame(150,6400,68.5,0.3,0,0.12,600,250).save('mockup_t+150.png')
    m=frame(37,640,2.4,1.7,9,0.93,600,400); m.save('mockup_t+37.png')
    ref=Image.open(sys.argv[1]).convert('RGB').resize((W,H),Image.LANCZOS)
    c=Image.new('RGB',(W,H*2+40),(20,20,20)); c.paste(ref,(0,0)); c.paste(m,(0,H+40))
    ImageDraw.Draw(c).text((20,H+5),'TOP: reference   BOTTOM: SPXWebcastOverlay mockup T+37',font=ImageFont.truetype(FB,28),fill='white'); c.save('compare.png')
    if os.path.exists('ref_frame_t+33.png'):
        real=Image.open('ref_frame_t+33.png').convert('RGB').crop((0,1660,3840,2160)).resize((1920,250),Image.LANCZOS)
        mk=frame(33,548,1.9,1.7,9,0.95,600,300,'USSF-385').crop((0,830,1920,1080))
        cf=Image.new('RGB',(1920,540),(20,20,20)); cf.paste(real,(0,0)); cf.paste(mk,(0,290))
        ImageDraw.Draw(cf).text((10,258),'TOP: real webcast (4K frame, T+33, scaled to 1080p)   BOTTOM: SPXWebcastOverlay mockup',font=ImageFont.truetype(FB,24),fill='white'); cf.save('compare_fade.png')
