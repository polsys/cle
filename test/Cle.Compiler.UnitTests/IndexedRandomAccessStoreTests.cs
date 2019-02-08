using System;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class IndexedRandomAccessStoreTests
    {
        [Test]
        public void Reserve_set_get_succeeds()
        {
            var store = new IndexedRandomAccessStore<string>();

            Assert.That(store.Count, Is.EqualTo(0));
            Assert.That(store.ReserveIndex(), Is.EqualTo(0));
            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(() => store[0] = "test", Throws.Nothing);
            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(store[0], Is.EqualTo("test"));
        }

        [Test]
        public void Elements_can_be_set_in_any_order()
        {
            var store = new IndexedRandomAccessStore<string>();

            Assert.That(store.ReserveIndex(), Is.EqualTo(0));
            Assert.That(store.ReserveIndex(), Is.EqualTo(1));
            Assert.That(store.ReserveIndex(), Is.EqualTo(2));
            Assert.That(() => store[2] = "something", Throws.Nothing);
            Assert.That(store[2], Is.EqualTo("something"));
        }

        [Test]
        public void Internal_array_is_resized()
        {
            var store = new IndexedRandomAccessStore<string>();

            for (var i = 0; i < 10; i++)
            {
                Assert.That(store.ReserveIndex(), Is.EqualTo(i));
                Assert.That(store.Count, Is.EqualTo(i + 1));
            }
            Assert.That(() => store[9] = "nine", Throws.Nothing);

            for (var i = 10; i < 1000; i++)
            {
                Assert.That(store.ReserveIndex(), Is.EqualTo(i));
                Assert.That(store.Count, Is.EqualTo(i + 1));
            }
            Assert.That(() => store[100] = "one zero zero", Throws.Nothing);
            Assert.That(() => store[999] = "nine nine nine", Throws.Nothing);

            Assert.That(store[9], Is.EqualTo("nine"));
            Assert.That(store[100], Is.EqualTo("one zero zero"));
            Assert.That(store[999], Is.EqualTo("nine nine nine"));
        }

        [Test]
        public void Reserve_must_be_called_before_setting()
        {
            var store = new IndexedRandomAccessStore<string>();

            Assert.That(() => store[0] = "test", Throws.InstanceOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Cannot_get_uninitialized_element_before_any_sets()
        {
            var store = new IndexedRandomAccessStore<string>();

            Assert.That(store.ReserveIndex(), Is.EqualTo(0));
            Assert.That(() => store[0], Throws.InstanceOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Cannot_get_uninitialized_element_after_a_different_set()
        {
            var store = new IndexedRandomAccessStore<string>();

            Assert.That(store.ReserveIndex(), Is.EqualTo(0));
            Assert.That(store.ReserveIndex(), Is.EqualTo(1));
            Assert.That(() => store[1] = "one", Throws.Nothing);
            Assert.That(() => store[0], Throws.InstanceOf<IndexOutOfRangeException>());
        }
    }
}
