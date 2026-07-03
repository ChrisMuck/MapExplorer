extends Node3D

const SQRT_3 := 1.7320508075688772
const HEX_DIRECTIONS: Array[Vector2i] = [
	Vector2i(1, 0),
	Vector2i(1, -1),
	Vector2i(0, -1),
	Vector2i(-1, 0),
	Vector2i(-1, 1),
	Vector2i(0, 1),
]

@export_range(3, 16) var map_radius := 8
@export_range(0.5, 2.0, 0.05) var hex_size := 1.0
@export_range(0.0, 0.12, 0.005) var camera_orbit_speed := 0.018
@export var map_seed := 4711

var terrain_materials: Dictionary = {}
var side_materials: Dictionary = {}
var accent_materials: Dictionary = {}
var feature_materials: Dictionary = {}
var terrain_by_coord: Dictionary = {}
var elevation_by_coord: Dictionary = {}
var rng := RandomNumberGenerator.new()
var camera_rig: Node3D
var water_plane: MeshInstance3D
var capture_preview := false
var capture_frame_count := 0


func _ready() -> void:
	rng.seed = map_seed
	_create_materials()
	_build_map()
	_build_overland_features()
	_build_water()
	_build_light()
	_build_camera()
	_build_world_environment()
	var args := OS.get_cmdline_args() + OS.get_cmdline_user_args()
	capture_preview = args.has("--capture-preview")


func _process(delta: float) -> void:
	if camera_rig != null:
		camera_rig.rotate_y(camera_orbit_speed * delta)
	if water_plane != null:
		water_plane.rotate_y(delta * 0.006)
	if capture_preview:
		_capture_preview_when_ready()


func _capture_preview_when_ready() -> void:
	capture_frame_count += 1
	if capture_frame_count < 8:
		return

	var texture := get_viewport().get_texture()
	if texture == null:
		return

	var image := texture.get_image()
	if image == null:
		return

	image.save_png("res://preview_hex_map.png")
	get_tree().quit()


func _create_materials() -> void:
	terrain_materials = {
		"water": _material(Color("#2c7895"), 0.48, 0.02, _noise_texture(Color("#22627c"), Color("#55a6bd"), 11, 0.34)),
		"coast": _material(Color("#c8a865"), 0.76, 0.0, _noise_texture(Color("#b78b43"), Color("#e3c987"), 12, 0.32)),
		"grass": _material(Color("#5f9949"), 0.84, 0.0, _noise_texture(Color("#467f38"), Color("#84bc64"), 13, 0.38)),
		"forest": _material(Color("#246541"), 0.88, 0.0, _noise_texture(Color("#17412d"), Color("#3f7e54"), 14, 0.34)),
		"hills": _material(Color("#817446"), 0.9, 0.0, _noise_texture(Color("#635c39"), Color("#aa9660"), 15, 0.36)),
		"mountain": _material(Color("#70776e"), 0.92, 0.0, _noise_texture(Color("#555c56"), Color("#9ba094"), 16, 0.26)),
		"snow": _material(Color("#c8d0cb"), 0.8, 0.0, _noise_texture(Color("#aeb8b1"), Color("#f0f2ec"), 17, 0.18)),
	}
	side_materials = {
		"water": _material(Color("#294e61"), 0.6, 0.0),
		"coast": _material(Color("#7c6536"), 0.8, 0.0),
		"grass": _material(Color("#355f32"), 0.88, 0.0),
		"forest": _material(Color("#173d2f"), 0.9, 0.0),
		"hills": _material(Color("#464833"), 0.92, 0.0),
		"mountain": _material(Color("#444a46"), 0.92, 0.0),
		"snow": _material(Color("#869089"), 0.84, 0.0),
	}
	accent_materials = {
		"sand_light": _overlay_material(Color("#dfbd78"), 0.1),
		"scrub": _overlay_material(Color("#81784c"), 0.08),
		"hill_light": _overlay_material(Color("#968856"), 0.08),
	}
	feature_materials = {
		"river_bank": _material(Color("#1d4a57"), 0.74, 0.0),
		"river_water": _emissive_material(Color("#216f91"), Color("#2ea7d0"), 0.12),
		"river_foam": _overlay_material(Color("#9edce2"), 0.18),
		"road_shadow": _overlay_material(Color("#2d241a"), 0.24),
		"road": _material(Color("#aa895d"), 0.9, 0.0, _noise_texture(Color("#79613e"), Color("#ccb082"), 41, 0.22)),
		"road_center": _overlay_material(Color("#d6bd8a"), 0.14),
		"terrace_line": _overlay_material(Color("#1f2b25"), 0.22),
		"territory_border": _emissive_material(Color("#116b8e"), Color("#29b9f2"), 0.22),
		"settlement_wall": _material(Color("#776f5d"), 0.86, 0.0),
		"settlement_roof": _material(Color("#6b3046"), 0.74, 0.0),
		"settlement_light": _emissive_material(Color("#d5a652"), Color("#ffc95c"), 0.16),
	}


func _material(albedo: Color, roughness: float, metallic: float, albedo_texture: Texture2D = null) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = albedo
	material.albedo_texture = albedo_texture
	material.roughness = roughness
	material.metallic = metallic
	material.specular_mode = BaseMaterial3D.SPECULAR_SCHLICK_GGX
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	return material


func _overlay_material(albedo: Color, alpha: float) -> StandardMaterial3D:
	var material := _material(albedo, 0.9, 0.0)
	material.albedo_color.a = alpha
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return material


func _emissive_material(albedo: Color, emission: Color, energy: float) -> StandardMaterial3D:
	var material := _material(albedo, 0.62, 0.0)
	material.emission_enabled = true
	material.emission = emission
	material.emission_energy_multiplier = energy
	return material


func _noise_texture(base: Color, accent: Color, seed_offset: int, strength: float) -> Texture2D:
	var size := 96
	var image := Image.create(size, size, true, Image.FORMAT_RGBA8)
	var texture_noise := FastNoiseLite.new()
	texture_noise.seed = map_seed + seed_offset * 997
	texture_noise.frequency = 0.07
	texture_noise.fractal_octaves = 4
	texture_noise.fractal_gain = 0.52

	for y in range(size):
		for x in range(size):
			var soft_noise: float = texture_noise.get_noise_2d(x, y) * 0.5 + 0.5
			var fine_grain := sin(float(x) * 0.73 + float(y) * 0.31 + float(seed_offset)) * 0.045
			var mix_amount: float = clamp(soft_noise * strength + fine_grain + 0.08, 0.0, 1.0)
			image.set_pixel(x, y, base.lerp(accent, mix_amount))

	image.generate_mipmaps()
	return ImageTexture.create_from_image(image)


func _build_map() -> void:
	terrain_by_coord.clear()
	elevation_by_coord.clear()

	var noise := FastNoiseLite.new()
	noise.seed = map_seed
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.frequency = 0.18
	noise.fractal_octaves = 4
	noise.fractal_gain = 0.48

	for q in range(-map_radius, map_radius + 1):
		var r_min: int = max(-map_radius, -q - map_radius)
		var r_max: int = min(map_radius, -q + map_radius)
		for r in range(r_min, r_max + 1):
			var coord := Vector2i(q, r)
			var world_pos := _axial_to_world(coord)
			var radial_fade: float = clamp(world_pos.length() / float(map_radius * 1.7), 0.0, 1.0)
			var height_noise: float = noise.get_noise_2d(q, r) - radial_fade * 0.08
			var terrain := _pick_terrain(height_noise, coord)
			var elevation := _terrain_elevation(terrain, height_noise)
			terrain_by_coord[coord] = terrain
			elevation_by_coord[coord] = elevation
			var tile := _create_hex_tile(coord, elevation, terrain)
			tile.name = "Hex_%s_%s_%s" % [q, r, terrain]
			add_child(tile)
			tile.position = Vector3(world_pos.x, 0.0, world_pos.y)
			_add_detail(tile, terrain, elevation, coord)


func _pick_terrain(value: float, coord: Vector2i) -> String:
	var polar_bias := sin(float(coord.x) * 0.53) * 0.04 + cos(float(coord.y) * 0.71) * 0.03
	var h := value + polar_bias
	if h < -0.38:
		return "water"
	if h < -0.24:
		return "coast"
	if h < 0.12:
		return "grass"
	if h < 0.29:
		return "forest"
	if h < 0.46:
		return "hills"
	if h < 0.62:
		return "mountain"
	return "snow"


func _terrain_elevation(terrain: String, value: float) -> float:
	var raw_elevation := 0.3
	match terrain:
		"water":
			return 0.06
		"coast":
			return 0.18
		"grass":
			raw_elevation = 0.34 + max(value, 0.0) * 0.28
		"forest":
			raw_elevation = 0.48 + max(value, 0.0) * 0.34
		"hills":
			raw_elevation = 0.76 + value * 0.36
		"mountain":
			raw_elevation = 1.08 + value * 0.48
		"snow":
			raw_elevation = 1.36 + value * 0.48
	return snapped(raw_elevation, 0.18)


func _create_hex_tile(coord: Vector2i, elevation: float, terrain: String) -> Node3D:
	var tile := Node3D.new()
	var top_mesh := MeshInstance3D.new()
	var side_mesh := MeshInstance3D.new()
	var rim_mesh := MeshInstance3D.new()

	top_mesh.name = "Top"
	top_mesh.mesh = _make_hex_top_mesh(hex_size * 0.96, elevation)
	top_mesh.material_override = terrain_materials[terrain]
	tile.add_child(top_mesh)

	side_mesh.name = "CliffSides"
	side_mesh.mesh = _make_hex_side_mesh(hex_size * 0.96, elevation)
	side_mesh.material_override = side_materials[terrain]
	tile.add_child(side_mesh)

	rim_mesh.name = "StoneRim"
	rim_mesh.mesh = _make_hex_ring_mesh(hex_size * 0.982, hex_size * 0.94, elevation + 0.012)
	rim_mesh.material_override = _material(Color("#2f3d35"), 0.92, 0.0)
	tile.add_child(rim_mesh)

	for accent in _make_surface_accents(terrain, elevation, coord):
		tile.add_child(accent)

	for terrace in _make_terrace_marks(terrain, elevation):
		tile.add_child(terrace)

	return tile


func _make_hex_top_mesh(radius: float, y: float) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(6):
		var a := _hex_corner(radius, i)
		var b := _hex_corner(radius, (i + 1) % 6)
		st.set_uv(Vector2(0.5, 0.5))
		st.set_normal(Vector3.UP)
		st.add_vertex(Vector3(0.0, y, 0.0))
		st.set_uv(_top_uv(b, radius))
		st.set_normal(Vector3.UP)
		st.add_vertex(Vector3(b.x, y, b.y))
		st.set_uv(_top_uv(a, radius))
		st.set_normal(Vector3.UP)
		st.add_vertex(Vector3(a.x, y, a.y))
	st.generate_normals()
	return st.commit()


func _make_hex_side_mesh(radius: float, y: float) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(6):
		var a := _hex_corner(radius, i)
		var b := _hex_corner(radius, (i + 1) % 6)
		var top_a := Vector3(a.x, y, a.y)
		var top_b := Vector3(b.x, y, b.y)
		var bottom_a := Vector3(a.x, -0.26, a.y)
		var bottom_b := Vector3(b.x, -0.26, b.y)
		st.set_uv(Vector2(float(i) / 6.0, 0.0))
		st.add_vertex(top_a)
		st.set_uv(Vector2(float(i + 1) / 6.0, 0.0))
		st.add_vertex(top_b)
		st.set_uv(Vector2(float(i) / 6.0, 1.0))
		st.add_vertex(bottom_a)
		st.set_uv(Vector2(float(i + 1) / 6.0, 0.0))
		st.add_vertex(top_b)
		st.set_uv(Vector2(float(i + 1) / 6.0, 1.0))
		st.add_vertex(bottom_b)
		st.set_uv(Vector2(float(i) / 6.0, 1.0))
		st.add_vertex(bottom_a)
	st.generate_normals()
	return st.commit()


func _make_hex_ring_mesh(outer_radius: float, inner_radius: float, y: float) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(6):
		var outer_a := _hex_corner(outer_radius, i)
		var outer_b := _hex_corner(outer_radius, (i + 1) % 6)
		var inner_a := _hex_corner(inner_radius, i)
		var inner_b := _hex_corner(inner_radius, (i + 1) % 6)
		st.set_uv(_top_uv(outer_a, outer_radius))
		st.add_vertex(Vector3(outer_a.x, y, outer_a.y))
		st.set_uv(_top_uv(outer_b, outer_radius))
		st.add_vertex(Vector3(outer_b.x, y, outer_b.y))
		st.set_uv(_top_uv(inner_a, outer_radius))
		st.add_vertex(Vector3(inner_a.x, y, inner_a.y))
		st.set_uv(_top_uv(outer_b, outer_radius))
		st.add_vertex(Vector3(outer_b.x, y, outer_b.y))
		st.set_uv(_top_uv(inner_b, outer_radius))
		st.add_vertex(Vector3(inner_b.x, y, inner_b.y))
		st.set_uv(_top_uv(inner_a, outer_radius))
		st.add_vertex(Vector3(inner_a.x, y, inner_a.y))
	st.generate_normals()
	return st.commit()


func _hex_corner(radius: float, index: int) -> Vector2:
	var angle := deg_to_rad(60.0 * float(index) + 30.0)
	return Vector2(cos(angle) * radius, sin(angle) * radius)


func _top_uv(point: Vector2, radius: float) -> Vector2:
	return Vector2(point.x / (radius * 2.0) + 0.5, point.y / (radius * 2.0) + 0.5)


func _axial_to_world(coord: Vector2i) -> Vector2:
	var x := hex_size * SQRT_3 * (float(coord.x) + float(coord.y) * 0.5)
	var y := hex_size * 1.5 * float(coord.y)
	return Vector2(x, y)


func _make_surface_accents(terrain: String, elevation: float, coord: Vector2i) -> Array[Node3D]:
	var accents: Array[Node3D] = []
	var local_rng := RandomNumberGenerator.new()
	local_rng.seed = absi(coord.x * 928371 + coord.y * 364479 + map_seed * 101)
	var material_names: Array[String] = []

	match terrain:
		"coast":
			material_names = ["sand_light", "scrub"]
		"hills":
			material_names = ["hill_light", "scrub"]
		_:
			return accents

	var count := 1
	if terrain == "coast" and local_rng.randf() > 0.45:
		return accents
	if terrain == "hills" and local_rng.randf() > 0.32:
		return accents

	for i in range(count):
		var patch := MeshInstance3D.new()
		patch.name = "PaintedTerrainAccent"

		var mesh := CylinderMesh.new()
		mesh.top_radius = local_rng.randf_range(0.32, 0.58) * hex_size
		mesh.bottom_radius = mesh.top_radius
		mesh.height = 0.008
		mesh.radial_segments = 14
		patch.mesh = mesh

		var angle := local_rng.randf_range(0.0, TAU)
		var distance := local_rng.randf_range(0.08, 0.44) * hex_size
		patch.position = Vector3(cos(angle) * distance, elevation + 0.022 + float(i) * 0.004, sin(angle) * distance)
		patch.rotation_degrees.y = local_rng.randf_range(0.0, 360.0)
		patch.scale.z = local_rng.randf_range(0.45, 0.78)
		patch.material_override = accent_materials[material_names[i % material_names.size()]]
		accents.append(patch)

	return accents


func _make_terrace_marks(terrain: String, elevation: float) -> Array[Node3D]:
	var marks: Array[Node3D] = []
	if terrain in ["water", "coast"]:
		return marks

	var ring_count := clampi(roundi((elevation - 0.32) / 0.36), 0, 3)
	for i in range(ring_count):
		var ring := MeshInstance3D.new()
		ring.name = "PaintedElevationContour"
		var outer_radius := (0.77 - float(i) * 0.15) * hex_size
		var inner_radius := outer_radius - 0.018 * hex_size
		ring.mesh = _make_hex_ring_mesh(outer_radius, inner_radius, elevation + 0.026 + float(i) * 0.002)
		ring.material_override = feature_materials["terrace_line"]
		marks.append(ring)

	return marks


func _build_overland_features() -> void:
	_build_river([
		Vector2i(-8, 1),
		Vector2i(-7, 1),
		Vector2i(-6, 0),
		Vector2i(-5, 0),
		Vector2i(-4, 1),
		Vector2i(-3, 1),
		Vector2i(-2, 2),
		Vector2i(-1, 2),
		Vector2i(0, 1),
		Vector2i(1, 1),
		Vector2i(2, 0),
		Vector2i(3, 0),
		Vector2i(4, -1),
		Vector2i(5, -1),
		Vector2i(6, -2),
		Vector2i(7, -2),
	])
	_build_river([
		Vector2i(-2, -6),
		Vector2i(-1, -6),
		Vector2i(0, -6),
		Vector2i(0, -5),
		Vector2i(1, -5),
		Vector2i(1, -4),
		Vector2i(2, -4),
		Vector2i(2, -3),
		Vector2i(3, -3),
		Vector2i(4, -4),
	])

	var settlements: Array[Vector2i] = [
		Vector2i(-5, 2),
		Vector2i(-1, 0),
		Vector2i(3, -2),
		Vector2i(4, 3),
		Vector2i(-3, 5),
	]
	for settlement in settlements:
		_build_settlement(settlement)

	_build_road([
		Vector2i(-5, 2),
		Vector2i(-4, 2),
		Vector2i(-3, 2),
		Vector2i(-2, 1),
		Vector2i(-1, 0),
		Vector2i(0, 0),
		Vector2i(1, -1),
		Vector2i(2, -1),
		Vector2i(3, -2),
	])
	_build_road([
		Vector2i(-1, 0),
		Vector2i(-1, 1),
		Vector2i(0, 2),
		Vector2i(1, 2),
		Vector2i(2, 2),
		Vector2i(3, 2),
		Vector2i(4, 3),
	])
	_build_road([
		Vector2i(-5, 2),
		Vector2i(-5, 3),
		Vector2i(-4, 4),
		Vector2i(-3, 5),
	])
	_build_territory_border([
		Vector2i(-6, 3),
		Vector2i(-5, 2),
		Vector2i(-4, 2),
		Vector2i(-3, 1),
		Vector2i(-2, 1),
		Vector2i(-1, 0),
		Vector2i(0, 0),
		Vector2i(1, -1),
		Vector2i(2, -1),
		Vector2i(3, -2),
		Vector2i(4, -2),
	])


func _build_river(coords: Array[Vector2i]) -> void:
	var points := _coords_to_path_points(coords, 0.055, 0.18, 2101)
	if points.size() < 2:
		return

	var bank := MeshInstance3D.new()
	bank.name = "OrganicRiverBank"
	bank.mesh = _make_ribbon_mesh(points, 0.36 * hex_size)
	bank.material_override = feature_materials["river_bank"]
	add_child(bank)

	var river := MeshInstance3D.new()
	river.name = "OrganicRiver"
	river.mesh = _make_ribbon_mesh(_raise_points(points, 0.018), 0.23 * hex_size)
	river.material_override = feature_materials["river_water"]
	add_child(river)

	var foam := MeshInstance3D.new()
	foam.name = "RiverHighlight"
	foam.mesh = _make_ribbon_mesh(_raise_points(points, 0.034), 0.045 * hex_size)
	foam.material_override = feature_materials["river_foam"]
	add_child(foam)


func _build_road(coords: Array[Vector2i]) -> void:
	var points := _coords_to_path_points(coords, 0.075, 0.2, 3307)
	if points.size() < 2:
		return

	var shadow := MeshInstance3D.new()
	shadow.name = "RoadBed"
	shadow.mesh = _make_ribbon_mesh(points, 0.26 * hex_size)
	shadow.material_override = feature_materials["road_shadow"]
	add_child(shadow)

	var road := MeshInstance3D.new()
	road.name = "OrganicRoad"
	road.mesh = _make_ribbon_mesh(_raise_points(points, 0.012), 0.16 * hex_size)
	road.material_override = feature_materials["road"]
	add_child(road)

	var center := MeshInstance3D.new()
	center.name = "RoadWornCenter"
	center.mesh = _make_ribbon_mesh(_raise_points(points, 0.024), 0.045 * hex_size)
	center.material_override = feature_materials["road_center"]
	add_child(center)


func _build_territory_border(coords: Array[Vector2i]) -> void:
	var points := _coords_to_path_points(coords, 0.12, 0.16, 5297)
	if points.size() < 2:
		return

	var border := MeshInstance3D.new()
	border.name = "BlueTerritoryBorder"
	border.mesh = _make_dashed_path_mesh(points, 0.07 * hex_size, 0.74)
	border.material_override = feature_materials["territory_border"]
	add_child(border)


func _build_settlement(coord: Vector2i) -> void:
	if not _coord_exists(coord):
		return

	var settlement := Node3D.new()
	settlement.name = "MapSettlement_%s_%s" % [coord.x, coord.y]
	var world_pos := _axial_to_world(coord)
	var elevation := _surface_height(coord)
	settlement.position = Vector3(world_pos.x, elevation + 0.045, world_pos.y)
	add_child(settlement)

	var plaza := MeshInstance3D.new()
	var plaza_mesh := CylinderMesh.new()
	plaza_mesh.top_radius = 0.42 * hex_size
	plaza_mesh.bottom_radius = 0.42 * hex_size
	plaza_mesh.height = 0.05
	plaza_mesh.radial_segments = 6
	plaza.mesh = plaza_mesh
	plaza.material_override = feature_materials["settlement_wall"]
	settlement.add_child(plaza)

	for i in range(4):
		var house := MeshInstance3D.new()
		var house_mesh := BoxMesh.new()
		house_mesh.size = Vector3(0.2, 0.16 + float(i % 2) * 0.06, 0.18)
		house.mesh = house_mesh
		var angle := float(i) * TAU / 4.0 + 0.38
		house.position = Vector3(cos(angle) * 0.22, 0.11, sin(angle) * 0.22)
		house.rotation_degrees.y = rad_to_deg(angle) + 28.0
		house.material_override = feature_materials["settlement_wall"]
		settlement.add_child(house)

		var roof := MeshInstance3D.new()
		var roof_mesh := CylinderMesh.new()
		roof_mesh.top_radius = 0.01
		roof_mesh.bottom_radius = 0.15
		roof_mesh.height = 0.14
		roof_mesh.radial_segments = 4
		roof.mesh = roof_mesh
		roof.position = house.position + Vector3(0.0, house_mesh.size.y * 0.5 + 0.08, 0.0)
		roof.rotation_degrees = Vector3(0.0, house.rotation_degrees.y + 45.0, 0.0)
		roof.material_override = feature_materials["settlement_roof"]
		settlement.add_child(roof)

	var beacon := OmniLight3D.new()
	beacon.name = "SettlementWarmth"
	beacon.light_color = Color("#ffc86b")
	beacon.light_energy = 0.24
	beacon.omni_range = 2.0
	beacon.position.y = 0.5
	settlement.add_child(beacon)


func _coords_to_path_points(coords: Array[Vector2i], y_offset: float, jitter: float, seed_offset: int) -> Array[Vector3]:
	var points: Array[Vector3] = []
	for i in range(coords.size()):
		var coord := coords[i]
		if not _coord_exists(coord):
			continue

		var world_pos := _axial_to_world(coord)
		var local_rng := RandomNumberGenerator.new()
		local_rng.seed = absi(coord.x * 761393 + coord.y * 193496 + map_seed * 997 + seed_offset + i * 37)
		var jitter_vec := Vector2(local_rng.randf_range(-jitter, jitter), local_rng.randf_range(-jitter, jitter)) * hex_size
		var height := _surface_height(coord) + y_offset
		points.append(Vector3(world_pos.x + jitter_vec.x, height, world_pos.y + jitter_vec.y))

	return _smooth_path_points(points, 5)


func _smooth_path_points(points: Array[Vector3], subdivisions: int) -> Array[Vector3]:
	if points.size() < 3:
		return points

	var smooth: Array[Vector3] = []
	for i in range(points.size() - 1):
		var p0 := points[maxi(i - 1, 0)]
		var p1 := points[i]
		var p2 := points[i + 1]
		var p3 := points[mini(i + 2, points.size() - 1)]
		for step in range(subdivisions):
			var t := float(step) / float(subdivisions)
			smooth.append(_catmull_rom(p0, p1, p2, p3, t))
	smooth.append(points[points.size() - 1])
	return smooth


func _catmull_rom(p0: Vector3, p1: Vector3, p2: Vector3, p3: Vector3, t: float) -> Vector3:
	var t2 := t * t
	var t3 := t2 * t
	return (p1 * 2.0 + (p2 - p0) * t + (p0 * 2.0 - p1 * 5.0 + p2 * 4.0 - p3) * t2 + (-p0 + p1 * 3.0 - p2 * 3.0 + p3) * t3) * 0.5


func _raise_points(points: Array[Vector3], amount: float) -> Array[Vector3]:
	var raised: Array[Vector3] = []
	for point in points:
		raised.append(point + Vector3(0.0, amount, 0.0))
	return raised


func _make_ribbon_mesh(points: Array[Vector3], width: float) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	if points.size() < 2:
		return st.commit()

	var left: Array[Vector3] = []
	var right: Array[Vector3] = []
	for i in range(points.size()):
		var previous := points[maxi(i - 1, 0)]
		var next := points[mini(i + 1, points.size() - 1)]
		var tangent := Vector3(next.x - previous.x, 0.0, next.z - previous.z).normalized()
		if tangent.length() <= 0.001:
			tangent = Vector3.FORWARD
		var side := Vector3(-tangent.z, 0.0, tangent.x) * width * 0.5
		left.append(points[i] + side)
		right.append(points[i] - side)

	for i in range(points.size() - 1):
		var u0 := float(i) / float(points.size() - 1)
		var u1 := float(i + 1) / float(points.size() - 1)
		_add_ribbon_quad(st, left[i], left[i + 1], right[i], right[i + 1], u0, u1)

	st.generate_normals()
	return st.commit()


func _make_dashed_path_mesh(points: Array[Vector3], width: float, dash_fraction: float) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	if points.size() < 2:
		return st.commit()

	for i in range(points.size() - 1):
		if i % 2 == 1:
			continue
		var start := points[i]
		var end := points[i].lerp(points[i + 1], dash_fraction)
		var tangent := Vector3(end.x - start.x, 0.0, end.z - start.z).normalized()
		if tangent.length() <= 0.001:
			continue
		var side := Vector3(-tangent.z, 0.0, tangent.x) * width * 0.5
		_add_ribbon_quad(st, start + side, end + side, start - side, end - side, 0.0, 1.0)

	st.generate_normals()
	return st.commit()


func _add_ribbon_quad(st: SurfaceTool, left_a: Vector3, left_b: Vector3, right_a: Vector3, right_b: Vector3, u0: float, u1: float) -> void:
	st.set_uv(Vector2(u0, 0.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(left_a)
	st.set_uv(Vector2(u1, 0.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(left_b)
	st.set_uv(Vector2(u0, 1.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(right_a)

	st.set_uv(Vector2(u0, 1.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(right_a)
	st.set_uv(Vector2(u1, 0.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(left_b)
	st.set_uv(Vector2(u1, 1.0))
	st.set_normal(Vector3.UP)
	st.add_vertex(right_b)


func _surface_height(coord: Vector2i) -> float:
	return float(elevation_by_coord.get(coord, 0.3))


func _coord_exists(coord: Vector2i) -> bool:
	return elevation_by_coord.has(coord)


func _add_detail(tile: Node3D, terrain: String, elevation: float, coord: Vector2i) -> void:
	match terrain:
		"forest":
			for i in range(3 + int(abs(coord.x + coord.y) % 3)):
				tile.add_child(_make_tree(elevation, i))
		"hills":
			for i in range(2):
				tile.add_child(_make_rock(elevation, i))
		"mountain", "snow":
			tile.add_child(_make_mountain(elevation, terrain == "snow"))
		"coast":
			if abs(coord.x * 3 + coord.y * 5) % 7 == 0:
				tile.add_child(_make_banner(elevation))


func _make_tree(elevation: float, index: int) -> Node3D:
	var tree := Node3D.new()
	tree.name = "Tree"
	var trunk := MeshInstance3D.new()
	var crown := MeshInstance3D.new()
	var angle := float(index) * 2.1 + rng.randf_range(-0.35, 0.35)
	var distance := rng.randf_range(0.18, 0.52) * hex_size
	tree.position = Vector3(cos(angle) * distance, elevation + 0.04, sin(angle) * distance)
	tree.rotation_degrees.y = rng.randf_range(0.0, 360.0)

	var trunk_mesh := CylinderMesh.new()
	trunk_mesh.top_radius = 0.045
	trunk_mesh.bottom_radius = 0.06
	trunk_mesh.height = 0.28
	trunk_mesh.radial_segments = 6
	trunk.mesh = trunk_mesh
	trunk.position.y = 0.14
	trunk.material_override = _material(Color("#4c3327"), 0.82, 0.0)
	tree.add_child(trunk)

	var crown_mesh := CylinderMesh.new()
	crown_mesh.bottom_radius = 0.19
	crown_mesh.top_radius = 0.035
	crown_mesh.height = 0.46
	crown_mesh.radial_segments = 7
	crown.mesh = crown_mesh
	crown.position.y = 0.43
	crown.material_override = _material(Color("#214c34"), 0.86, 0.0)
	tree.add_child(crown)

	return tree


func _make_rock(elevation: float, index: int) -> Node3D:
	var rock := MeshInstance3D.new()
	rock.name = "Rock"
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.22, 0.14 + index * 0.06, 0.28)
	rock.mesh = mesh
	rock.position = Vector3(-0.3 + index * 0.45, elevation + mesh.size.y * 0.5, 0.08 - index * 0.23)
	rock.rotation_degrees = Vector3(0.0, 25.0 + index * 40.0, 7.0)
	rock.material_override = _material(Color("#74786c"), 0.9, 0.0)
	return rock


func _make_mountain(elevation: float, snowy: bool) -> Node3D:
	var mountain := Node3D.new()
	mountain.name = "Mountain"
	var base := MeshInstance3D.new()
	var peak := MeshInstance3D.new()

	var base_mesh := CylinderMesh.new()
	base_mesh.bottom_radius = 0.52
	base_mesh.top_radius = 0.06
	base_mesh.height = 1.0
	base_mesh.radial_segments = 6
	base.mesh = base_mesh
	base.position.y = elevation + 0.5
	base.rotation_degrees.y = 30.0
	base.material_override = _material(Color("#6f746f"), 0.92, 0.0)
	mountain.add_child(base)

	var peak_mesh := CylinderMesh.new()
	peak_mesh.bottom_radius = 0.23
	peak_mesh.top_radius = 0.0
	peak_mesh.height = 0.32
	peak_mesh.radial_segments = 6
	peak.mesh = peak_mesh
	peak.position.y = elevation + 0.98
	peak.rotation_degrees.y = 30.0
	peak.material_override = _material(Color("#d4dbd8") if snowy else Color("#b6bbb6"), 0.72, 0.0)
	mountain.add_child(peak)

	return mountain


func _make_banner(elevation: float) -> Node3D:
	var banner := Node3D.new()
	banner.name = "CoastMarker"
	var pole := MeshInstance3D.new()
	var flag := MeshInstance3D.new()

	var pole_mesh := CylinderMesh.new()
	pole_mesh.top_radius = 0.025
	pole_mesh.bottom_radius = 0.025
	pole_mesh.height = 0.72
	pole_mesh.radial_segments = 6
	pole.mesh = pole_mesh
	pole.position = Vector3(0.12, elevation + 0.36, -0.18)
	pole.material_override = _material(Color("#5a4730"), 0.8, 0.0)
	banner.add_child(pole)

	var flag_mesh := BoxMesh.new()
	flag_mesh.size = Vector3(0.34, 0.2, 0.035)
	flag.mesh = flag_mesh
	flag.position = Vector3(0.28, elevation + 0.58, -0.18)
	flag.material_override = _material(Color("#ba4f45"), 0.62, 0.0)
	banner.add_child(flag)

	return banner


func _build_water() -> void:
	var mesh := PlaneMesh.new()
	mesh.size = Vector2(map_radius * hex_size * 4.4, map_radius * hex_size * 4.4)
	water_plane = MeshInstance3D.new()
	water_plane.name = "DistantWaterPlane"
	water_plane.mesh = mesh
	water_plane.position.y = -0.42
	water_plane.material_override = _material(Color("#1d4858"), 0.32, 0.0)
	add_child(water_plane)


func _build_light() -> void:
	var sun := DirectionalLight3D.new()
	sun.name = "LateAfternoonSun"
	sun.light_energy = 1.25
	sun.shadow_enabled = true
	sun.directional_shadow_mode = DirectionalLight3D.SHADOW_ORTHOGONAL
	sun.rotation_degrees = Vector3(-48.0, -38.0, 0.0)
	add_child(sun)

	var fill := OmniLight3D.new()
	fill.name = "SoftBlueFill"
	fill.light_color = Color("#7fa0b3")
	fill.light_energy = 0.28
	fill.omni_range = 28.0
	fill.position = Vector3(-8.0, 9.0, 6.0)
	add_child(fill)


func _build_camera() -> void:
	camera_rig = Node3D.new()
	camera_rig.name = "SlowOrbitRig"
	add_child(camera_rig)

	var camera := Camera3D.new()
	camera.name = "StrategyCamera"
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 16.0
	camera.near = 0.05
	camera.far = 120.0
	camera.position = Vector3(10.6, 13.8, 13.2)
	camera_rig.add_child(camera)
	camera.look_at(Vector3(0.0, 0.35, 0.0), Vector3.UP)
	camera.current = true


func _build_world_environment() -> void:
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("#2b3237")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("#899188")
	environment.ambient_light_energy = 0.18
	environment.fog_enabled = true
	environment.fog_light_color = Color("#b6b8aa")
	environment.fog_density = 0.006
	environment.fog_sky_affect = 0.25
	environment.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	environment.tonemap_exposure = 0.85
	environment.glow_enabled = false

	var world := WorldEnvironment.new()
	world.name = "WorldEnvironment"
	world.environment = environment
	add_child(world)
