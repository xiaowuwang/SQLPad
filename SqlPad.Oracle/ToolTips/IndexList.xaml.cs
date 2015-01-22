﻿using System.Collections.Generic;
using System.Windows;

namespace SqlPad.Oracle.ToolTips
{
	public partial class IndexList
	{
		public IndexList()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty IndexesProperty = DependencyProperty.Register("Indexes", typeof(IEnumerable<IndexDetailsModel>), typeof(IndexList), new FrameworkPropertyMetadata(PropertyChangedCallbackHandler));

		public IEnumerable<IndexDetailsModel> Indexes
		{
			get { return (IEnumerable<IndexDetailsModel>)GetValue(IndexesProperty); }
			set { SetValue(IndexesProperty, value); }
		}

		private static void PropertyChangedCallbackHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var constraintList = (IndexList)dependencyObject;
			constraintList.DataGrid.ItemsSource = (IEnumerable<IndexDetailsModel>)args.NewValue;
		}
	}
}
