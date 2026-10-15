from PIL import Image, ImageDraw
def icon(sz):
    S=16; N=sz*S; im=Image.new('RGBA',(N,N),(0,0,0,0)); d=ImageDraw.Draw(im)
    P=lambda x,y:(x*N,y*N); W=(255,255,255,255)
    cx=0.5; bw=0.055 if sz>=32 else 0.065; fw=bw*1.3
    # fairing (ogive) + body
    d.polygon([P(cx,0.03),P(cx+fw*0.7,0.09),P(cx+fw,0.16),P(cx+fw,0.27),P(cx-fw,0.27),P(cx-fw,0.16),P(cx-fw*0.7,0.09)],fill=W)
    d.rectangle([P(cx-bw,0.27),P(cx+bw,0.78)],fill=W)
    # interstage gap (dark band) and grid fins
    d.rectangle([P(cx-bw,0.38),P(cx+bw,0.40)],fill=(0,0,0,0))
    d.rectangle([P(cx-bw-0.06,0.33),P(cx-bw,0.355)],fill=W); d.rectangle([P(cx+bw,0.33),P(cx+bw+0.06,0.355)],fill=W)
    # landing legs
    d.polygon([P(cx-bw,0.64),P(cx-bw-0.015,0.64),P(cx-bw-0.08,0.82),P(cx-bw-0.05,0.82),P(cx-bw,0.71)],fill=W)
    d.polygon([P(cx+bw,0.64),P(cx+bw+0.015,0.64),P(cx+bw+0.08,0.82),P(cx+bw+0.05,0.82),P(cx+bw,0.71)],fill=W)
    # webcast timeline motif: line + passed dot + now dot + upcoming hollow dot
    d.rectangle([P(0.06,0.895),P(0.94,0.925)],fill=(255,255,255,200))
    r=0.045; d.ellipse([P(0.22-r,0.91-r),P(0.22+r,0.91+r)],fill=W)
    r=0.07; d.ellipse([P(0.5-r,0.91-r),P(0.5+r,0.91+r)],fill=W)
    r=0.045; d.ellipse([P(0.78-r,0.91-r),P(0.78+r,0.91+r)],fill=(40,40,40,255),outline=W,width=int(N*0.025))
    return im.resize((sz,sz),Image.LANCZOS)
for s in (38,24): icon(s).save(f'pkg/GameData/SPXWebcastOverlay/Icons/f9_{s}.png')
