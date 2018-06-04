using System;


namespace BeatThat
{
	public class TypeAndInterface  
	{
		public TypeAndInterface(Type t) :this(t, t)
		{
		}

		public TypeAndInterface(Type t, Type i)
		{
			this.concreteType = t;
			this.interfaceType = i;
		}
		
		public Type concreteType
		{
			get; set;
		}
		
		public Type interfaceType
		{
			get; set;
		}
	}
}
