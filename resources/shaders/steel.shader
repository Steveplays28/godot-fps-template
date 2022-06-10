shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec4 steel_color : hint_color;
uniform sampler2D rust_texture : hint_albedo;
uniform float roughness : hint_range(0, 1) = 0.5f;
uniform float specular : hint_range(0, 1) = 0.5f;
uniform float metallic : hint_range(0, 1) = 1f;
uniform sampler2D steel_normal_texture : hint_normal;
uniform sampler2D rust_normal_texture : hint_normal;
uniform float rust_mix_amount : hint_range(0, 1) = 0.25f;
uniform vec3 uv1_scale = vec3(1f, 1f, 1f);
uniform vec3 uv1_offset;

void vertex() {
	UV = UV * uv1_scale.xy + uv1_offset.xy;
}

void fragment() {
	ALBEDO = mix(steel_color, texture(rust_texture, UV), rust_mix_amount).rgb;
	METALLIC = metallic * rust_mix_amount;
	ROUGHNESS = roughness;
	SPECULAR = specular;
	NORMALMAP = mix(texture(steel_normal_texture, UV), texture(rust_normal_texture, UV), 0.5f).rgb;
	NORMALMAP_DEPTH = uv1_scale.x * uv1_scale.y;
}
