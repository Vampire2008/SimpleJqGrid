namespace SimpleJqGrid
{
	public interface IJqGridRow<T> : IJqGridRow<T, int>
		where T : class, new()
	{ }

	public interface IJqGridRow<T, TKey> : ISimpleJqGridRow<TKey>
		where T : class, new()
	{
		void CopyFrom(T source);
		void ApplyTo(T dest);
	}

	public interface ISimpleJqGridRow : ISimpleJqGridRow<int>
	{

	}

	public interface ISimpleJqGridRow<TKey>
	{
		TKey Id { get; set; }
	}
}