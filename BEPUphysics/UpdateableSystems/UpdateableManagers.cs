using BEPUphysics.Threading;

namespace BEPUphysics.UpdateableSystems
{

    ///<summary>
    /// Manages updateables that update during the forces stage.
    ///</summary>
    public class DuringForcesUpdateableManager : UpdateableManager<IDuringForcesUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public DuringForcesUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public DuringForcesUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }


    }

    ///<summary>
    /// Manages updateables that update before the narrow phase.
    ///</summary>
    public class BeforeNarrowPhaseUpdateableManager : UpdateableManager<IBeforeNarrowPhaseUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public BeforeNarrowPhaseUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public BeforeNarrowPhaseUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

    }

    ///<summary>
    /// Manages updateables that update before the solver.
    ///</summary>
    public class BeforeSolverUpdateableManager : UpdateableManager<IBeforeSolverUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public BeforeSolverUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public BeforeSolverUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

    }

    ///<summary>
    /// Manages updateables that update at the end of a time step.
    ///</summary>
    public class BeforePositionUpdateUpdateableManager : UpdateableManager<IBeforePositionUpdateUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public BeforePositionUpdateUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public BeforePositionUpdateUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }


    }

    ///<summary>
    /// Manages updateables that update at the end of a time step.
    ///</summary>
    public class EndOfTimeStepUpdateableManager : UpdateableManager<IEndOfTimeStepUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public EndOfTimeStepUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public EndOfTimeStepUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }


    }

    ///<summary>
    /// Manages updateables that update at the end of a frame.
    ///</summary>
    public class EndOfFrameUpdateableManager : UpdateableManager<IEndOfFrameUpdateable>
    {
        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public EndOfFrameUpdateableManager(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
        }

        ///<summary>
        /// Constructs a manager.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public EndOfFrameUpdateableManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
        }

        protected override void MultithreadedUpdate(int i)
        {
            if (simultaneouslyUpdatedUpdateables[i].IsUpdating)
                simultaneouslyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }

        protected override void SequentialUpdate(int i)
        {
            if (sequentiallyUpdatedUpdateables[i].IsUpdating)
                sequentiallyUpdatedUpdateables[i].Update(timeStepSettings.TimeStepDuration);
        }


    }
}
