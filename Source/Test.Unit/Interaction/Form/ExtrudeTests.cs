﻿using System.IO;
using Macad.Common;
using Macad.Test.Utils;
using Macad.Core.Shapes;
using Macad.Interaction;
using Macad.Interaction.Editors.Shapes;
using Macad.Occt;
using NUnit.Framework;
using System.Windows.Input;
using Macad.Core;

namespace Macad.Test.Unit.Interaction.Form
{
    [TestFixture]
    public class ExtrudeTests
    {
        const string _BasePath = @"Interaction\Form\Extrude";

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
        public void CreateExtrude()
        {
            var ctx = Context.Current;

            var sketch = TestSketchGenerator.CreateSketch();
            var body = TestGeomGenerator.CreateBody(sketch, new Pnt(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();
            ctx.MoveTo(90, 250);

            var tool = new CreateExtrudeTool(body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "CreateExtrude01"));
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.Null);
        }

        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void CreateExtrudeSolid()
        {
            var ctx = Context.Current;

            var shape = TestGeomGenerator.CreateImprint();
            ctx.WorkspaceController.Selection.SelectEntity(shape.Body);
            ctx.ViewportController.ZoomFitAll();
            ctx.MoveTo(90, 250);

            var tool = new CreateExtrudeTool(shape.Body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            ctx.MoveTo(90, 250);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "CreateExtrudeSolid01"));

            ctx.SelectAt(90, 260);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "CreateExtrudeSolid02"));
            var extrude = shape.Body.Shape as Extrude;
            Assert.IsNotNull( extrude );
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.Null);
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void ReselectExtrudeSolid()
        {
            var ctx = Context.Current;

            var shape = TestGeomGenerator.CreateImprint();
            ctx.WorkspaceController.Selection.SelectEntity(shape.Body);
            ctx.ViewportController.ZoomFitAll();
            ctx.MoveTo(90, 250);

            var tool = new CreateExtrudeTool(shape.Body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            ctx.SelectAt(90, 260);
            var extrude = shape.Body.Shape as Extrude;
            Assert.IsNotNull( extrude );
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.Null);

            tool = new CreateExtrudeTool(extrude);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            ctx.SelectAt(250, 250);
            AssertHelper.IsSameViewport(Path.Combine(_BasePath, "CreateExtrudeSolid10"));
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.Null);
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void ExtrudeFromNotSelectable()
        {
            var ctx = Context.Current;

            var sketch = TestSketchGenerator.CreateSketch();
            var body = TestGeomGenerator.CreateBody(sketch, new Pnt(10, 10, 0));
            ctx.WorkspaceController.Selection.SelectEntity(body);
            ctx.ViewportController.ZoomFitAll();
            ctx.WorkspaceController.Invalidate(false, true);

            // Create Extrude from sketch
            var tool = new CreateExtrudeTool(body);
            Assert.That(ctx.WorkspaceController.StartTool(tool));
            Assert.That(ctx.WorkspaceController.CurrentTool, Is.Null);
            ctx.WorkspaceController.Invalidate(false, true);

            // Make sure that the whole shape is selectable
            ctx.WorkspaceController.Selection.SelectEntity(null);
            ctx.WorkspaceController.Invalidate(false, true);
            ctx.SelectAt(250, 250);
            Assert.AreEqual(1, ctx.WorkspaceController.Selection.SelectedEntities.Count);
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void EditorIdle()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            var editor = Editor.CreateEditor(extrude);
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
        public void LiveDepth()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            var oldDepth = extrude.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                ctx.MoveTo(250, 216);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepth01"));

                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 189);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepth02"));
            
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepth03"));
                Assert.Greater(extrude.Depth, oldDepth);

                // Cleanup
                editor.Stop();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepth99"));
            });
        }

        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthChangeSign()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            extrude.Body.Rotation = new Quaternion(0, -45.0.ToRad(), 0);
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            var oldDepth = extrude.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                // Pos to Neg
                ctx.MoveTo(270, 217);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(222, 289);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeSign01"));
            
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeSign02"));
                Assert.Less(extrude.Depth, oldDepth);
                
                // Neg to Pos
                ctx.MoveTo(197, 336);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(261, 229);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeSign03"));
            
                ctx.ViewportController.MouseUp();
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthChangeSign04"));
                Assert.Greater(extrude.Depth, oldDepth);

                // Cleanup
                editor.Stop();
            });
        }

        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthClamp()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            extrude.Body.Rotation = new Quaternion(0, -45.0.ToRad(), 0);
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            ctx.ViewportController.ZoomFitAll();
            ctx.WorkspaceController.Workspace.GridStep = 1.0;

            Assert.Multiple(() =>
            {
                ctx.MoveTo(270, 217);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(280, 180);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthClamp01"));
                ctx.MoveTo(280, 180, ModifierKeys.Control);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthClamp02"));
                ctx.ViewportController.MouseUp();
                Assert.AreEqual(3.0, extrude.Depth);

                // Cleanup
                editor.Stop();
            });
        }
        
        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthSymmetric()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            extrude.Body.Rotation = new Quaternion(0, -45.0.ToRad(), 0);
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            ctx.ViewportController.ZoomFitAll();
            ctx.WorkspaceController.Workspace.GridStep = 1.0;

            Assert.Multiple(() =>
            {
                ctx.MoveTo(270, 217);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(280, 180);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSymmetric01"));
                ctx.MoveTo(280, 180, ModifierKeys.Shift);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSymmetric02"));
                ctx.MoveTo(280, 180);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSymmetric01"));
                ctx.ViewportController.MouseUp();

                // Cleanup
                editor.Stop();
            });
        }

        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthSolid()
        {
            var shape = TestGeomGenerator.CreateImprint();
            var subshapeRef = shape.GetSubshapeReference(SubshapeType.Face, 7);
            var extrude = Extrude.Create(shape.Body, subshapeRef);

            var ctx = Context.Current;
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            var oldDepth = extrude.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                ctx.MoveTo(250, 180);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSolid01"));

                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 170);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSolid02"));
            
                ctx.ViewportController.MouseUp();
                Assert.Greater(extrude.Depth, oldDepth);

                // Cleanup
                editor.Stop();
            });
        }
        
        //--------------------------------------------------------------------------------------------------

        [Test]
        public void LiveDepthSolidChangeSign()
        {
            var shape = TestGeomGenerator.CreateImprint();
            var subshapeRef = shape.GetSubshapeReference(SubshapeType.Face, 7);
            var extrude = Extrude.Create(shape.Body, subshapeRef);

            var ctx = Context.Current;
            var editor = Editor.CreateEditor(extrude);
            editor.Start();

            var oldDepth = extrude.Depth;
            ctx.ViewportController.ZoomFitAll();

            Assert.Multiple(() =>
            {
                // Pos to Neg
                ctx.MoveTo(250, 180);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 250);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSolidChangeSign01"));

                ctx.ViewportController.MouseUp();
                ctx.MoveTo(250, 250);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSolidChangeSign02"));
                Assert.Less(extrude.Depth, oldDepth);

                // Neg to Pos
                ctx.MoveTo(250, 230);
                ctx.ViewportController.MouseDown();
                ctx.MoveTo(250, 160);
                AssertHelper.IsSameViewport(Path.Combine(_BasePath, "LiveDepthSolidChangeSign03"));

                ctx.ViewportController.MouseUp();
                Assert.AreEqual(oldDepth, extrude.Depth, 0.0001);

                // Cleanup
                editor.Stop();
            });
        }

        //--------------------------------------------------------------------------------------------------
        
        [Test]
        public void LiveDepthUndo()
        {
            var ctx = Context.Current;

            var extrude = TestGeomGenerator.CreateExtrude();
            var editor = Editor.CreateEditor(extrude);
            editor.Start();
            ctx.UndoHandler.Commit();
            Assert.AreEqual(1, ctx.UndoHandler.UndoStack.Count);

            var oldDepth = extrude.Depth;
            ctx.ViewportController.ZoomFitAll();
            ctx.MoveTo(250, 216);
            ctx.ViewportController.MouseDown();
            ctx.MoveTo(250, 189);
            ctx.ViewportController.MouseUp();
            editor.Stop();

            Assert.Greater(extrude.Depth, oldDepth);
            Assert.AreEqual(2, ctx.UndoHandler.UndoStack.Count);

            ctx.UndoHandler.DoUndo(1);

            Assert.AreEqual(extrude.Depth, oldDepth);
            Assert.AreEqual(1, ctx.UndoHandler.UndoStack.Count);
        }

        //--------------------------------------------------------------------------------------------------
    }
}