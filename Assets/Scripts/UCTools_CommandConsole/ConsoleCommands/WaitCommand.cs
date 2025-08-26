using UCTools_CommandConsole.Enums;

namespace UCTools_CommandConsole
{
    /// <summary>
    /// Wait command that pauses command execution for specified number of frames
    /// Useful for scripting and automated testing scenarios
    /// </summary>
    public class WaitCommand : ConsoleCommandBase
    {
        public override string CommandName => "wait";
        public override string Description => "Wait for next frame or specified number of frames";
        public override CategoryEnum Category => CategoryEnum.Scripting;

        public override void Execute(string[] args, IConsoleContext context)
        {
            if (args.Length == 0)
            {
                context.SetWaitFrames(1);
                context.WriteLine("Waiting 1 frame...");
            }
            else if (args.Length == 1)
            {
                if (TryParseInt(args[0], out int frames, context, "frame count"))
                {
                    if (frames < 0)
                    {
                        context.WriteError("Frame count must be non-negative");
                        return;
                    }
                
                    context.SetWaitFrames(frames);
                    context.WriteLine($"Waiting {frames} frame(s)...");
                }
            }
            else
            {
                context.WriteError(GetUsage());
            }
        }

        public override string GetUsage()
        {
            return "Usage: wait [n]\n" +
                   "  wait    - Wait for 1 frame\n" +
                   "  wait n  - Wait for n frames";
        }

        public override bool ValidateArgs(string[] args)
        {
            if (args.Length > 1) return false;
        
            if (args.Length == 1)
            {
                return int.TryParse(args[0], out int frames) && frames >= 0;
            }
        
            return true;
        }
    }
}