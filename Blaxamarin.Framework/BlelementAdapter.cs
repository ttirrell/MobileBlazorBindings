﻿using Emblazon;
using System.Diagnostics;
using System.Linq;
using Xamarin.Forms;

namespace Blaxamarin.Framework
{
    /// <summary>
    /// Represents a "shadow" item that Blazor uses to map changes into the live Xamarin.Forms Element tree.
    /// </summary>
    public class BlelementAdapter : EmblazonAdapter<Element>
    {
        public BlelementAdapter(Element physicalParent) : base(physicalParent)
        {
        }

        protected override EmblazonAdapter<Element> CreateAdapter(Element physicalParent)
        {
            return new BlelementAdapter(physicalParent);
        }

        public override void RemovePhysicalControl(Element control)
        {
            // TODO: Need to make this logic more generic; not all parents are Layouts, not all children are Views
            var physicalParent = control.Parent;
            var physicalParentAsLayout = physicalParent as Layout<View>;
            var childTargetAsView = control as View;

            physicalParentAsLayout.Children.Remove(childTargetAsView);
        }

        protected override bool IsParented(Element nativeControl)
        {
            return nativeControl.Parent != null;
        }

        public override void AddPhysicalControl(Element parent, Element child, int physicalSiblingIndex)
        {
            // TODO: What is the set of types that support child elements? Do they all need to be special-cased here? (Maybe...)

            switch (parent)
            {
                case Layout<View> parentAsLayout:
                    {
                        var childAsView = child as View;

                        if (physicalSiblingIndex <= parentAsLayout.Children.Count)
                        {
                            parentAsLayout.Children.Insert(physicalSiblingIndex, childAsView);
                        }
                        else
                        {
                            Debug.WriteLine($"WARNING: {nameof(AddPhysicalControl)} called with {nameof(physicalSiblingIndex)}={physicalSiblingIndex}, but parentAsLayout.Children.Count={parentAsLayout.Children.Count}");
                            parentAsLayout.Children.Add(childAsView);
                        }
                    }
                    break;
                case ContentView parentAsContentView:
                    {
                        var childAsView = child as View;
                        parentAsContentView.Content = childAsView;
                    }
                    break;
                case Application parentAsApp:
                    {
                        if (parentAsApp.MainPage != null)
                        {
                            Debug.Fail($"Application already has MainPage set; cannot set {parentAsApp.GetType().FullName}'s MainPage to {child.GetType().FullName}");
                        }
                        else
                        {
                            if (child is Page childControlAsPage)
                            {
                                parentAsApp.MainPage = childControlAsPage;
                            }
                            else
                            {
                                // TODO: Do we need a BlelementAdapter representing the dummy ContentPage? Or should the Razor page be a ContentPage somehow?
                                var dummyView = new ContentPage
                                {
                                    Content = child as View
                                };
                                parentAsApp.MainPage = dummyView;
                                //Debug.Fail($"Application MainPage must be a Page; cannot set {parentAsApp.GetType().FullName}'s MainPage to {childControl.GetType().FullName}");
                            }
                        }
                    }
                    break;
                default:
                    Debug.Fail($"Don't know how to handle parent element type {parent.GetType().FullName} in order to add child {child.GetType().FullName}");
                    break;
            }
        }

        public override int GetPhysicalSiblingIndex(Element nativeComponent)
        {
            // TODO: What is the set of types that support child elements? Do they all need to be special-cased here? (Maybe...)

            switch (nativeComponent.Parent)
            {
                case Layout<View> parentAsLayout:
                    {
                        var childAsView = nativeComponent as View;

                        return parentAsLayout.Children.IndexOf(childAsView);
                    }
                case ContentView _:
                    {
                        // A ContentView can have only 1 child, so its index is always 0. Not that anyone
                        // should typically need the sibling index here, because this component can't
                        // ever *have* any siblings...
                        return 0;
                    }
                case Application _:
                    {
                        // An Application can have only 1 child (its MainPage), so its index is always 0. Not that anyone
                        // should typically need the sibling index here, because this component can't
                        // ever *have* any siblings...
                        return 0;
                    }
                default:
                    Debug.Fail($"Don't know how to handle parent element type {nativeComponent.Parent.GetType().FullName} in order to get index of sibling {nativeComponent.GetType().FullName}");
                    return -1;
            }
        }
    }
}
