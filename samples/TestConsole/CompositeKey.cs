namespace TestConsole
{
    public readonly struct CompositeKey
    {
        public CompositeKey(int userId, short companyId)
        {
            this.UserId = userId;
            this.CompanyId = companyId;
        }

        public int UserId { get; }
        public short CompanyId { get; }
    }
}
