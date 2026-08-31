from PIL import Image, ImageDraw

def rounded(size):
    S = size*4
    img = Image.new("RGBA",(S,S),(0,0,0,0))
    d = ImageDraw.Draw(img)
    r = int(S*0.22)
    # teal gradient bg
    top=(24,196,214); bot=(9,120,150)
    for y in range(S):
        t=y/S
        c=(int(top[0]+(bot[0]-top[0])*t),int(top[1]+(bot[1]-top[1])*t),int(top[2]+(bot[2]-top[2])*t),255)
        d.line([(0,y),(S,y)],fill=c)
    mask=Image.new("L",(S,S),0)
    ImageDraw.Draw(mask).rounded_rectangle([0,0,S-1,S-1],radius=r,fill=255)
    img.putalpha(mask)
    dd=ImageDraw.Draw(img)
    # lightning bolt
    w,h=S,S
    bolt=[(0.58,0.12),(0.30,0.55),(0.47,0.55),(0.40,0.88),(0.72,0.42),(0.53,0.42),(0.66,0.12)]
    pts=[(x*w,y*h) for x,y in bolt]
    dd.polygon(pts,fill=(255,255,255,255))
    return img.resize((size,size),Image.LANCZOS)

sizes=[16,32,48,64,128,256]
imgs=[rounded(s) for s in sizes]
imgs[-1].save("app/icon.ico",format="ICO",sizes=[(s,s) for s in sizes])
for s in [16,32,48,96,128]:
    rounded(s).save(f"extension/icons/icon{s}.png")
print("icons done")
