extends Button

@onready var Count_Label: Label = $CountLabel
@onready var Item_TextureRect: TextureRect = $ItemIcon
@onready var Rarity_TextureRect: TextureRect = $RarityIcon
@onready var Connecting_TextureRect: TextureRect = $ConnectingIcon
@onready var Colour_ColorRect: ColorRect = $ColorRect

func _ready() -> void:
    set_item(50, 1, false, 5) #TEST
    set_item_count(12) #TEST

func set_item(icon_index: int, rarity: int, connecting: bool, colour: int) -> void:
    Item_TextureRect.texture = Util.get_item_icon(icon_index)

    Connecting_TextureRect.visible = connecting

    match rarity:
        1:
            Rarity_TextureRect.show()
            Rarity_TextureRect.texture = load("res://Graphics/BlockModifier/1star.png")
        2:
            Rarity_TextureRect.show()
            Rarity_TextureRect.texture = load("res://Graphics/BlockModifier/2star.png")
        3:
            Rarity_TextureRect.show()
            Rarity_TextureRect.texture = load("res://Graphics/BlockModifier/3star.png")
        _:
            Rarity_TextureRect.hide()
    
    match colour:
        1:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_WHITE)
        2:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_BLACK)
        3:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_PURPLE)
        4:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_PINK)
        5:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_RED)
        6:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_GREEN)
        7:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_YELLOW)
        8:
            Colour_ColorRect.show()
            Colour_ColorRect.color = Color(Constants.COLOUR_BLUE)
        _:
            Colour_ColorRect.hide()
    
func set_item_count(count: int) -> void:
    Count_Label.text = str(count)