using Mirror;

public enum SoundEvent
{
    Attack,
    Hit,
    Skill,
}

/// <summary>
/// This class inherits a class in Mirror, I override part of its function.
/// </summary>
public struct PlaySoundMessage : NetworkMessage
{
    public SoundEvent eventType;
    public int characterIndex;
}