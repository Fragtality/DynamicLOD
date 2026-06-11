namespace DynamicLOD.AppConfig
{
    public class SimPointerDefinition
    {
        public virtual SimVariant SimVariant { get; set; }
        public virtual string SimProcess { get; set; }
        public virtual string SimModule { get; set; }
        public virtual long OffsetModuleBase { get; set; }
        public virtual long OffsetPointer { get; set; }
        public virtual long OffsetPointerTlod { get; set; }
        public virtual long OffsetPointerTlodVr { get; set; }
        public virtual long OffsetPointerOlod { get; set; }
        public virtual long OffsetPointerOlodVr { get; set; }
    }
}
