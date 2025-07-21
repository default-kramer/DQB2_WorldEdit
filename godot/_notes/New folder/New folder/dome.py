import os
from PIL import Image

images = [i for i in os.listdir() if i.lower().endswith(".png")]
for i in images:
    image = Image.open(i)
    if image.height == 256 and image.width == 256:
        image.crop((0,0,128,128)).save(f"output/{i[:-4]}_1.png")
        image.crop((128,0,256,128)).save(f"output/{i[:-4]}_2.png")
        image.crop((0,128,128,256)).save(f"output/{i[:-4]}_3.png")
        image.crop((128,128,256,256)).save(f"output/{i[:-4]}_4.png")
        continue
    if image.height == image.width:
        image.save(os.path.join("output",i))
        continue
    if image.height == image.width * 2:
        image.crop((0,0,image.width,image.width)).save(f"output/{i[:-4]}_1.png")
        image.crop((0,image.width,image.width,image.height)).save(f"output/{i[:-4]}_2.png")
        continue
    if image.width == image.height * 1.5:
        image.crop((0,0,image.height / 2,image.height / 2)).save(f"output/{i[:-4]}_1.png")
        image.crop((image.height / 2,0,image.height,image.height / 2)).save(f"output/{i[:-4]}_2.png")
        image.crop((image.height,0,image.width,image.height / 2)).save(f"output/{i[:-4]}_3.png")
        image.crop((0,image.height / 2,image.height / 2,image.height)).save(f"output/{i[:-4]}_4.png")
        image.crop((image.height / 2,image.height / 2,image.height,image.height)).save(f"output/{i[:-4]}_5.png")
        image.crop((image.height,image.height / 2,image.width,image.height)).save(f"output/{i[:-4]}_6.png")
        continue