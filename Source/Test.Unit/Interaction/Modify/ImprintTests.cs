﻿using System.IO;
using Macad.Test.Utils;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction.Editors.Shapes;
using Macad.Occt;
using NUnit.Framework;
using Macad.Common;
using Macad.Core;
using Macad.Interaction;
using System.Windows.Input;

namespace Macad.Test.Unit.Interaction.Modify
{
    [TestFixture]
    public class ImprintTests
    {
        const string _BasePath = @"Interaction\Modify\Imprint";

        //--------------------------------------------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            Context.InitWithView(500);
        }

        [TearDown]
        public void TearDown()
        {
            Context.Current.Deinit();
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void CreateImprint()
        {
            var ctx = Context.Current;

            var body = TestGeomGenerator.CreateBox().Body;
            TransformUtils.Translate(body, new Vec(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();

            var tool = new CreateImprintTool(body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));

            ctx.MoveTo(90, 250);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "Create1"));

            ctx.SelectAt(90, 250);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "Create2"));
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.TypeOf(typeof(SketchEditorTool)));
        }
        
        //--------------------------------------------------------------------------------------------------

        [Test]
        public void SelectionFilterOnCreate()
        {
            var ctx = Context.Current;

            var body = TestGeomGenerator.CreateImprint().Body;
            TransformUtils.Translate(body, new Vec(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();

            var tool = new CreateImprintTool(body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));

            ctx.MoveTo(90, 250);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "SelectionFilterOnCreate1"));

            ctx.MoveTo(200, 277);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "SelectionFilterOnCreate2"));
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void ImprintReselectTargetFace()
        {
            var ctx = Context.Current;
            var body = TestGeomGenerator.CreateBox().Body;
            TransformUtils.Translate(body, new Vec(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();

            // Build imprint
            var tool = new CreateImprintTool(body);
            Assume.That(ctx.WorkspaceController.StartTool(tool));
            ctx.SelectAt(90, 250);
            var sketchTool = ctx.WorkspaceController.CurrentTool as SketchEditorTool;
            Assume.That(sketchTool != null);

            Assert.Multiple(() =>
            {
                sketchTool.StartSegmentCreation<SketchSegmentCircleCreator>();
                ctx.SelectAt(250, 250);
                ctx.SelectAt(150, 250);
                sketchTool.Stop();
                ctx.MoveTo(250, 250);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "ReselectTargetFace1"));

                // Start reselection, then cancel it
                ctx.WorkspaceController.StartTool(new CreateImprintTool(body.Shape as Imprint));
                ctx.MoveTo(300, 250);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "ReselectTargetFace2"));
                ctx.WorkspaceController.CancelTool(ctx.WorkspaceController.CurrentTool, true);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "ReselectTargetFace4"));

                // Start reselection, perform
                ctx.WorkspaceController.StartTool(new CreateImprintTool(body.Shape as Imprint));
                ctx.SelectAt(300, 250);
                AssertHelper.IsSameViewport(@Path.Combine(_BasePath, "ReselectTargetFace3"));
                Assert.IsNull(ctx.WorkspaceController.CurrentTool);
            });
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void SketchPropertyPanels()
        {
            var ctx = Context.Current;
            var propPanels = ctx.EnablePropertyPanels();
            
            var body = TestGeomGenerator.CreateBox().Body;
            TransformUtils.Translate(body, new Vec(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();

            var tool = new CreateImprintTool(body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            ctx.SelectAt(90, 250);
            Assert.IsInstanceOf<SketchEditorTool>(ctx.WorkspaceController.CurrentTool);

            // Body, BodyShape, Imprint, Sketch, SketchPoints, SketchSegments, SketchConstraints
            Assert.AreEqual(7, propPanels.Count);
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void EditorIdle()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            var editor = Editor.CreateEditor(imprint);
            editor.Start();
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "EditorIdle01"));
            
                // Cleanup
                editor.Stop();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "EditorIdle99"));
            });
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthRaise()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Mode = Imprint.ImprintMode.Raise;
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            var oldDepth = imprint.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                ctx.MoveTo(250, 191);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthRaise01"));

                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 170);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthRaise02"));
            
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthRaise03"));
                Assert.Greater(imprint.Depth, oldDepth);

                // Cleanup
                editor.Stop();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthRaise99"));
            });
        }
        
        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthLower()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Mode = Imprint.ImprintMode.Lower;
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            var oldDepth = imprint.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                ctx.MoveTo(250, 191);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthLower01"));

                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 227);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthLower02"));
            
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthLower03"));
                Assert.Greater(imprint.Depth, oldDepth);

                // Cleanup
                editor.Stop();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthLower99"));
            });
        }
        
        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthCutout()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Mode = Imprint.ImprintMode.Cutout;
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthCutout01"));
                imprint.Mode = Imprint.ImprintMode.Raise;
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthCutout02"));
                imprint.Mode = Imprint.ImprintMode.Cutout;
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthCutout03"));

                // Cleanup
                editor.Stop();
            });
        }
        
        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthRotate()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Mode = Imprint.ImprintMode.Raise;
            imprint.Body.Rotation = new Quaternion(0, -45.0.ToRad(), 0);
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            var oldDepth = imprint.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                ctx.MoveTo(290, 185);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(300, 166);
                ctx.ViewportController.MouseUp();

                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthRotate01"));
                Assert.Greater(imprint.Depth, oldDepth);

                // Cleanup
                editor.Stop();
            });
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthChangeMode()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Mode = Imprint.ImprintMode.Lower;
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                // Lower to Raise
                ctx.MoveTo(250, 208);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 170);
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeMode01"));
                Assert.AreEqual(Imprint.ImprintMode.Raise, imprint.Mode);
                
                // Raise to Lower
                ctx.MoveTo(250, 170);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 208);
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeMode02"));
                Assert.AreEqual(Imprint.ImprintMode.Lower, imprint.Mode);

                // Cleanup
                editor.Stop();
            });
        }

        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthClamp()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            imprint.Body.Rotation = new Quaternion(0, -45.0.ToRad(), 0);
            var editor = Editor.CreateEditor(imprint);
            editor.Start();

            ctx.ViewportController.ZoomFitAll();
            ctx.WorkspaceController.Workspace.GridStep = 1.0;

            Assert.Multiple(() =>
            {
                ctx.MoveTo(290, 185);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(300, 166);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthClamp01"));
                ctx.MoveTo(300, 166, ModifierKeys.Control);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthClamp02"));
                ctx.ViewportController.MouseUp();
                Assert.AreEqual(2.0, imprint.Depth);

                // Cleanup
                editor.Stop();
            });
        }
        
        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthUndo()
        {
            var ctx = Context.Current;

            var imprint = TestGeomGenerator.CreateImprint();
            var editor = Editor.CreateEditor(imprint);
            editor.Start();
            ctx.UndoHandler.Commit();
            Assert.AreEqual(1, ctx.UndoHandler.UndoStack.Count);

            var oldDepth = imprint.Depth;
            ctx.ViewportController.ZoomFitAll();
            ctx.MoveTo(250, 191);
            ctx.ViewportController.MouseDown();
            ctx.MoveTo(250, 170);
            ctx.ViewportController.MouseUp();
            editor.Stop();

            Assert.Greater(imprint.Depth, oldDepth);
            Assert.AreEqual(2, ctx.UndoHandler.UndoStack.Count);

            ctx.UndoHandler.DoUndo(1);

            Assert.AreEqual(imprint.Depth, oldDepth);
            Assert.AreEqual(1, ctx.UndoHandler.UndoStack.Count);
        }

        //--------------------------------------------------------------------------------------------------
    }
}