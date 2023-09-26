shader_type spatial;
//shader_type canvas_item;
render_mode unshaded, shadows_disabled;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

// sRGB => XYZ => D65_2_D60 => AP1 => RRT_SAT
const mat3 ACESInputMat = mat3(vec3(0.59719, 0.35458, 0.04823), vec3(0.07600, 0.90834, 0.01566), vec3(0.02840, 0.13383, 0.83777));
const mat3 ACESOutputMat = mat3(vec3(1.60475, -0.53108, -0.07367), vec3(-0.10208,  1.10813, -0.00605), vec3(-0.00327, -0.07276,  1.07602));

vec3 RRTAndODTFit(vec3 v)
{
    vec3 a = v * (v + 0.0245786f) - 0.000090537f;
    vec3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
    return a / b;
}

vec3 ACESFitted(vec3 color)
{
    color = color * ACESInputMat;

    // Apply RRT and ODT
    color = RRTAndODTFit(color);

    color = color * ACESOutputMat;

    // Clamp to [0, 1]
    color = clamp(color, 0.0, 1.0);

    return color;
}

// Cabbibo's HSV
vec3 hsv(float h, float s, float v)
{
  return mix( vec3( 1.0 ), clamp( ( abs( fract(
    h + vec3( 3.0, 2.0, 1.0 ) / 3.0 ) * 6.0 - 3.0 ) - 1.0 ), 0.0, 1.0 ), s ) * v;
}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
}

void fragment()
{
	vec2 uv = SCREEN_UV;
	uv *=  1.0 - uv.yx;
	
	vec4 color = texture(screen_texture, SCREEN_UV);
	/*vec4 lensFlare = texture(screen_texture, 1.0 - SCREEN_UV);
	
	lensFlare.rgb = pow(lensFlare.rgb, vec3(4.0)) / 4.0;
	
	float vig = uv.x * uv.y * 15.0;
	vig = pow(vig, 4.0);
	
	lensFlare *= vig;*/
	
	//if (color.r > 1.0 || color.g > 1.0 || color.b > 1.0)
	//{
	//	color.rgb = vec3(0.0, 0.0, 1.0);
	//}
	
	//color.rgb *= 2.0;
	
	//color.rgb += hsv(color.g * 0.1 + (TIME * 0.05), 1.0, 1.0) * 0.2;
	color.rgb = ACESFitted(color.rgb);
	
	
	ALBEDO = color.rgb;
	//ALPHA = 1.0 - color.a;
}
