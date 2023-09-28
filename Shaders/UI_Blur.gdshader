shader_type canvas_item;

uniform float blur : hint_range(0.0, 3.0);

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment()
{
	COLOR = texture(SCREEN_TEXTURE, SCREEN_UV, 3.0);
}
