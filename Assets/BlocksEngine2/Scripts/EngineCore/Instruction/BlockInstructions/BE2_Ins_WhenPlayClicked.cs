// BE2_Ins_WhenPlayClicked.cs
using MG_BlocksEngine2.Block.Instruction;

public class BE2_Ins_WhenPlayClicked : BE2_InstructionBase, I_BE2_Instruction {
    protected override void OnButtonPlay()
    {
        BlocksStack.IsActive = true;
    }

    protected override void OnAwake()
    {
        BlocksStack.OnStackLastBlockExecuted.AddListener(EndExecution);
    }

    void EndExecution()
    {
        BlocksStack.IsActive = false;
    }

    public new void Function()
    {
        ExecuteSection(0);
    }
}