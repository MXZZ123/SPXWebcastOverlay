# Approximate render of the settings window in KSP's HighLogic.Skin (dark grey window, grey gradient buttons,
# black text fields, round green toggles). Control order mirrors OverlayAddon.Window().
from PIL import Image, ImageDraw, ImageFont
SC=2; Wd=400; F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12*SC); FBd=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',12*SC)
FB16=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',15*SC)
im=Image.new('RGBA',((Wd+40)*SC,1100*SC),(0,0,0,0)); d=ImageDraw.Draw(im,'RGBA')
X0=20; Y=[0]; L=X0+10; R=X0+Wd-10
def p(*v): return [q*SC for q in v]
TXT=(224,224,224); DIM=(160,160,160)
def label(x,y,s,c=TXT,f=None): d.text(p(x,y),s,font=f or F,fill=c)
def button(x0,y0,x1,y1,s,c=TXT,f=None,enabled=True):
    for i in range(int((y1-y0)*SC)):
        t=i/((y1-y0)*SC); g=int(110-45*t) if enabled else int(75-25*t); d.line([(x0*SC,y0*SC+i),(x1*SC,y0*SC+i)],fill=(g,g,g+4))
    d.rounded_rectangle(p(x0,y0,x1,y1),radius=3*SC,outline=(25,25,25),width=SC)
    d.line([(x0*SC+2*SC,y0*SC+SC),(x1*SC-2*SC,y0*SC+SC)],fill=(150,150,155))
    f=f or F; w=d.textlength(s,font=f); h=f.size
    d.text(((x0+x1)/2*SC-w/2,(y0+y1)/2*SC-h*0.6),s,font=f,fill=c if enabled else (130,130,130))
def field(x0,y0,x1,y1,s):
    d.rectangle(p(x0,y0,x1,y1),fill=(12,12,12),outline=(85,85,85),width=SC); label(x0+4,y0+3,s,(200,230,200))
def toggle(x,y,on,s=''):
    d.ellipse(p(x,y+2,x+14,y+16),fill=(30,30,30),outline=(110,110,110),width=SC)
    if on: d.ellipse(p(x+3,y+5,x+11,y+13),fill=(120,220,90))
    if s: label(x+20,y+2,s)
def row(h): y=Y[0]; Y[0]+=h; return y
# window frame
TOP=10; Y[0]=TOP+28
def fieldrow(lbl,val):
    y=row(24); label(L,y+4,lbl); field(L+230,y+2,L+330,y+21,val)
y=row(42); button(L,y,R,y+36,'Overlay enabled: ON',(90,230,90),FB16)
row(6)
y=row(24); label(L,y+4,'Mission name'); field(L+110,y+2,R,y+21,'STARLINK 12-7')
y=row(20); label(L,y+2,'(empty = vessel name; shown uppercase under the clock)',DIM)
y=row(20); label(L,y+2,'Countdown / clock')
fieldrow('Countdown start (s before T-0)','10')
y=row(20); label(L,y+2,'Clock: T- 00:00:10')
y=row(26); button(L,y+2,(L+R)/2-2,y+24,'Start Countdown'); button((L+R)/2+2,y+2,R,y+24,'Reset')
y=row(22); toggle(L,y,True,'Auto-zero clock on liftoff')
y=row(22); toggle(L,y,False,'Hold at T-0 until liftoff')
row(6); y=row(20); label(L,y+2,'Timeline events (on / name / T+ seconds)')
evs=[('STARTUP',-3,1,1),('LIFTOFF',0,1,1),('MAX-Q',72,1,1),('MECO',145,1,1),('STAGE SEP',149,1,1),('BOOSTBACK',160,0,1),('ENTRY BURN',390,1,1),('LANDING BURN',480,1,1),('THROTTLE DOWN',50,1,0)]
sy0=Y[0]; boxh=260; d.rectangle(p(L-2,sy0,R,sy0+boxh),fill=(20,20,20),outline=(60,60,60),width=SC)
yy=sy0+4
for n,t,on,b in evs:
    if yy+24>sy0+boxh: break
    toggle(L+2,yy+1,on)
    if b: label(L+26,yy+4,n,TXT if on else DIM)
    else: field(L+26,yy+2,L+196,yy+21,n)
    field(L+200,yy+2,L+270,yy+21,f'{t:g}')
    if not b: button(L+276,yy+2,L+300,yy+22,'X')
    yy+=26
# scrollbar
d.rectangle(p(R-12,sy0+2,R-2,sy0+boxh-2),fill=(35,35,35)); d.rounded_rectangle(p(R-11,sy0+3,R-3,sy0+boxh*0.85),radius=3*SC,fill=(100,100,105))
Y[0]=sy0+boxh+4
y=row(28); button(L,y+2,L+120,y+24,'Add event'); button(L+126,y+2,R,y+24,'Reset events to defaults')
y=row(20); label(L,y+2,'(LIFTOFF can be hidden; the clock still zeroes at liftoff)',DIM)
y=row(22); toggle(L,y,False,'Static full timeline (first event .. last event + 30 s)')
fieldrow('Zoom to liftoff at T- (s)','6'); fieldrow('Pre-zoom window span (s)','640'); fieldrow('Zoomed / flight window span (s)','600')
row(6); y=row(20); label(L,y+2,'Gauge maxima')
fieldrow('Speed max (km/h)','8000'); fieldrow('Altitude max (km)','100'); fieldrow('G-force max','5')
row(6)
y=row(22); toggle(L,y,False,'Preview HUD (show with UI visible)')
y=row(22); toggle(L,y,False,'Also save settings per save game')
y=row(30); w=(R-L-8)/3
for i,s in enumerate(('Save','Defaults','Close')): button(L+i*(w+4),y+3,L+i*(w+4)+w,y+26,s)
BOT=Y[0]+10
# draw window background under everything
bgim=Image.new('RGBA',im.size,(0,0,0,0)); bd=ImageDraw.Draw(bgim)
bd.rounded_rectangle(p(X0,TOP,X0+Wd,BOT),radius=5*SC,fill=(38,40,43,245),outline=(95,98,102),width=SC)
bd.rounded_rectangle(p(X0+1,TOP+1,X0+Wd-1,TOP+24),radius=5*SC,fill=(52,55,58,255))
tw_=bd.textlength('SPX Webcast Overlay',font=FBd); bd.text(((X0+Wd/2)*SC-tw_/2,(TOP+6)*SC),'SPX Webcast Overlay',font=FBd,fill=(235,235,235))
out=Image.alpha_composite(bgim,im).crop((0,0,(Wd+40)*SC,(BOT+10)*SC))
bg=Image.new('RGBA',out.size,(70,90,110,255))
for yv in range(out.size[1]): ImageDraw.Draw(bg).line([(0,yv),(out.size[0],yv)],fill=(60+yv*30//out.size[1],80+yv*20//out.size[1],105,255))
Image.alpha_composite(bg,out).convert('RGB').resize(((Wd+40),(BOT+10)),Image.LANCZOS).save('preview_menu.png')
