using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class ScopedVariableMapTests
    {
        [Test]
        public void Variable_found_in_same_scope()
        {
            var map = new ScopedVariableMap();
            
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            Assert.That(map.TryAddVariable("b", 17), Is.True);
            Assert.That(map.TryGetVariable("a", out var a), Is.True);
            Assert.That(a, Is.EqualTo(1));
            Assert.That(map.TryGetVariable("b", out var b), Is.True);
            Assert.That(b, Is.EqualTo(17));
        }

        [Test]
        public void Variable_found_in_outer_scope()
        {
            var map = new ScopedVariableMap();
            
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            map.PushScope();
            Assert.That(map.TryGetVariable("a", out var index), Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void Variable_not_found_in_any_scope()
        {
            var map = new ScopedVariableMap();
            
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            map.PushScope();
            Assert.That(map.TryAddVariable("b", 2), Is.True);
            Assert.That(map.TryGetVariable("c", out _), Is.False);
        }

        [Test]
        public void Variable_visible_in_inner_but_not_outer_scope()
        {
            var map = new ScopedVariableMap();
            
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            map.PushScope();
            Assert.That(map.TryAddVariable("b", 2), Is.True);

            Assert.That(map.TryGetVariable("b", out var index), Is.True);
            Assert.That(index, Is.EqualTo(2));

            map.PopScope();
            Assert.That(map.TryGetVariable("b", out _), Is.False);
        }

        [Test]
        public void Cannot_add_same_name_to_same_scope()
        {
            var map = new ScopedVariableMap();

            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            Assert.That(map.TryAddVariable("a", 2), Is.False);
        }

        [Test]
        public void Cannot_add_same_name_to_inner_scope()
        {
            var map = new ScopedVariableMap();

            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 2), Is.False);
        }

        [Test]
        public void Can_add_variable_to_outer_scope_after_defining_in_inner()
        {
            // TODO: This behavior could be changed; for now it is simpler
            var map = new ScopedVariableMap();

            map.PushScope();
            map.PushScope();
            Assert.That(map.TryAddVariable("a", 1), Is.True);
            map.PopScope();
            Assert.That(map.TryAddVariable("a", 2), Is.True);
        }

        [Test]
        public void Cannot_do_operations_if_there_is_no_scope()
        {
            var map = new ScopedVariableMap();

            Assert.That(() => map.TryAddVariable("a", 0), Throws.InvalidOperationException);
            Assert.That(() => map.TryGetVariable("a", out _), Throws.InvalidOperationException);
            Assert.That(() => map.PopScope(), Throws.InvalidOperationException);
        }

        [Test]
        public void Pushing_and_popping_very_many_scopes()
        {
            var map = new ScopedVariableMap();
            const int scopeCount = 100;

            for (var i = 0; i < scopeCount; i++)
            {
                map.PushScope();
                map.TryAddVariable(i.ToString(), i);
            }
            for (var i = 0; i < scopeCount; i++)
            {
                map.PopScope();
            }

            // Now some of the new scopes should come from the cache
            // Verify that the cached scopes are empty
            for (var i = 0; i < scopeCount; i++)
            {
                map.PushScope();
            }
            for (var i = 0; i < scopeCount; i++)
            {
                Assert.That(map.TryGetVariable(i.ToString(), out _), Is.False);
            }
            for (var i = 0; i < scopeCount; i++)
            {
                map.PopScope();
            }
        }
    }
}
