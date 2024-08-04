using Godot;
using System;

public partial class Player : CharacterBody3D
{

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	public Vector3 direction;

	public Vector2 gpCamVector;
	public bool gamepadMode = true;

	private Marker3D cameraCenter {get; set;}
	private CollisionShape3D collisionBody {get; set;}

	private AnimationPlayer animP {get; set;}

	private AnimationTree animT {get; set;}
	[Export] float Animspeed = 60.0f;

	public Timer jumpTimer;

	[Export] float speed = 5.0f;
	[Export] float jumpVelocity = 5f;

	[Export] bool canWalk = true;

	[Export(PropertyHint.Range, "0.1,1.0")] float camSensitivity = 0.3f;
	[Export(PropertyHint.Range, "-90,0,1")] float minCamPitch = -50f;
	[Export(PropertyHint.Range, "0,90,1")] float maxCamPitch = 30f;
	
	public override void _Ready() {
		cameraCenter = GetNode<Marker3D>("camPivot");
		collisionBody = GetNode<CollisionShape3D>("Body");

		animT = GetNode<AnimationTree>("AnimationTree");

		animP = GetNode<AnimationPlayer>("AnimationPlayer");
		animP.Active = true;
	}

	public override void _Process(double delta)
	{
		//uncapturing the mouse disables PC movement but still simulates gravity
		if (Input.IsActionJustPressed("start")) Input.MouseMode = Input.MouseModeEnum.Visible;
		if (Input.IsMouseButtonPressed(MouseButton.Left)) Input.MouseMode = Input.MouseModeEnum.Captured;

		/**body rotation is in regular process because it lags in physicsprocess and is more a animation anyway
			maybe rotate extra collisions separately for invisible lag that may occur**/
		if (direction != Vector3.Zero  && canWalk && Input.MouseMode == Input.MouseModeEnum.Captured | gamepadMode)
		{
			Vector3 bodyRotation = collisionBody.Rotation;
			bodyRotation.Y = Mathf.LerpAngle(bodyRotation.Y,Mathf.Atan2(-direction.X, -direction.Z), (float)delta * speed);
			collisionBody.Rotation = bodyRotation;
		}
		//camera gamepad part (needs constant movement so _Input event wouldn't work with this like mouseinput)
		gpCamVector = Input.GetVector("cam_left", "cam_right", "cam_up", "cam_down");
		if(gpCamVector != Vector2.Zero)
		{
			Vector3 camRot = cameraCenter.RotationDegrees;
			camRot.Y -= gpCamVector.X * camSensitivity * (float)delta * 500;
			camRot.X -= gpCamVector.Y * camSensitivity * (float)delta * 500;
			camRot.X = Mathf.Clamp(camRot.X, minCamPitch, maxCamPitch); //prevents camera from going endlessly around the player
			cameraCenter.RotationDegrees = camRot;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor()){
			velocity.Y = jumpVelocity;
		}

		if (Input.IsActionPressed("run")){
			speed = 10.0f;
		}else{
			speed = 5.0f;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		direction = new Vector3(inputDir.X, 0, inputDir.Y).Rotated(Vector3.Up, GetNode<Marker3D>("camPivot").Rotation.Y).Normalized(); 
		if (direction != Vector3.Zero && canWalk && Input.MouseMode == Input.MouseModeEnum.Captured | gamepadMode )
		{
			velocity.X = direction.X * speed;
			velocity.Z = direction.Z * speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
		}

		isDead();
		Animator(inputDir);

		Velocity = velocity;
		MoveAndSlide();
	}

	public void Animator(Vector2 inputDir){
		animT.Set("parameters/moves/conditions/idle", inputDir == Vector2.Zero && IsOnFloor());
		animT.Set("parameters/moves/conditions/walk", inputDir != Vector2.Zero && speed == 5.0f && IsOnFloor() && canWalk);
		animT.Set("parameters/moves/conditions/run", inputDir != Vector2.Zero && speed > 5.0f && IsOnFloor() && canWalk);
		animT.Set("parameters/moves/conditions/jump", !IsOnFloor());
		animT.Set("parameters/moves/conditions/falling", IsOnFloor());

		if (Input.IsActionJustPressed("atack") && IsOnFloor()){
			animT.Set("parameters/atack/request", 1);
		}
	}

	public override void _Input(InputEvent @event)
	{
		gamepadMode = @event is InputEventJoypadButton | @event is InputEventJoypadMotion;
		Vector3 camRot = cameraCenter.RotationDegrees;
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			camRot.Y -= mouseMotion.Relative.X * camSensitivity;
			camRot.X -= mouseMotion.Relative.Y * camSensitivity;
		}
		camRot.X = Mathf.Clamp(camRot.X, minCamPitch, maxCamPitch); //prevents camera from going endlessly around the player
		cameraCenter.RotationDegrees = camRot;
	}

	public void animationFinished(string name){
		if(name == "Personagens/atack")
			canWalk = true;
	}


	public void isDead(){
		if(collisionBody.GlobalPosition.Y < -5){
			GetTree().Quit();
		}
	}
}
