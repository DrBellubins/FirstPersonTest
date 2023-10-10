using Godot;
using System;

public partial class WeaponSystem : Node3D
{
    [Export] public PackedScene[] Weapons;
    [Export] public RayCast3D Ray;
    [Export] public RichTextLabel AmmoDisplay;

    [Signal]
    public delegate void e_PickedUpEventHandler(int weaponIndex);

	private Weapon currentWeapon;
    private AnimationPlayer currentAnim;

    public override void _Ready()
	{
        e_PickedUp += pickedUp;
	}

	public override void _Process(double delta)
	{
        AmmoDisplay.Visible = (currentWeapon != null);

        if (currentWeapon != null)
        {
            AmmoDisplay.Text = $"{currentWeapon.CurrentMag} / {currentWeapon.CurrentAmmo}";

            if (Ray.IsColliding())
            {
                var hitPos = Ray.GetCollisionPoint();

                if (Input.IsActionJustPressed("shoot"))
                {
                    shoot();
                    drawDebugSphere(hitPos);
                }
            }

            if (Input.IsActionJustPressed("reload"))
                reload();
        }
	}

    private void shoot()
    {
        if (currentWeapon.CurrentMag > 0)
        {
            currentWeapon.CurrentMag--;
            currentAnim.Play("Fire");
        }
    }

    private void reload()
    {
        currentWeapon.CurrentAmmo -= (currentWeapon.MagazineSize - currentWeapon.CurrentMag);
        currentWeapon.CurrentMag = currentWeapon.MagazineSize;

        currentAnim.Play("Reload");
    }

    private void pickedUp(int weaponIndex)
	{
		currentWeapon = Weapons[weaponIndex].Instantiate<Weapon>();
        currentAnim = (AnimationPlayer)currentWeapon.GetChild(1);

        AddChild(currentWeapon);

        GD.PushWarning($"Picked up {currentWeapon.Name}");
    }

    private void drawDebugSphere(Vector3 pos)
    {
        var instance = new MeshInstance3D();

        instance.Position = pos;

        var mesh = new SphereMesh();
        mesh.Radius = 0.1f;
        mesh.Height = 0.2f;

        instance.Mesh = mesh;

        GetTree().CurrentScene.AddChild(instance);
    }
}
