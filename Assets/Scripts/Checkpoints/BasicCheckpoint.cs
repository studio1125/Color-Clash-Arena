public class BasicCheckpoint : Checkpoint {

    protected override void OnCheckpointDisabled() {

        // do nothing

    }

    protected override bool CheckRequirements() => true; // no requirements for basic checkpoint

}
