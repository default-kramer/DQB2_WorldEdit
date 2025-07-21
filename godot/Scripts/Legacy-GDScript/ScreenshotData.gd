extends SaveData
class_name ScreenshotData

const HeaderLength: int = 0x40

const ImageAddress: int = 0x69E90
const ImageSize: int = 0x64000

static var Instance: ScreenshotData
static func has_instance() -> bool:
	return Instance != null and Instance.IsLoaded

static func try_load_and_set(path: String) -> ScreenshotData:
	var screenshot_data: ScreenshotData = try_load(path)
	if screenshot_data != null:
		Instance = screenshot_data
		return screenshot_data
	return null
static func try_load(path: String) -> ScreenshotData:
	var screenshot_data: ScreenshotData = ScreenshotData.new()
	if screenshot_data._try_load(path, HeaderLength) == Error.OK:
		return screenshot_data
	return null

static func close() -> void:
	Instance = null

func make_instance() -> void:
	Instance = self

func get_image(index: int) -> Image:
	var image: Image = Image.new()
	image.load_jpg_from_buffer(Buffer.slice(ImageAddress + ImageSize * index, ImageAddress + ImageSize * index + ImageSize))
	return image
func set_image(index: int, filename: String) -> Error:
	var image: Image = Image.new()
	image.load(filename)
	
	var image_data = image.get_data()
	
	if (not (image_data[0] == 0xFF and image_data[1] == 0xD8)):
		return Error.FAILED
	if (len(image_data) > ImageSize):
		return Error.FAILED
	
	fill(0, ImageAddress + ImageSize * index, ImageSize)
	
	for i in range(len(image_data)):
		Buffer[ImageAddress + ImageSize * index + i] = image_data[i]
	
	return Error.OK
