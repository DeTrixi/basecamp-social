// Workaround for KeyboardAvoidingView class constructor type incompatibility
// with @types/react@19 JSX.ElementClass check.
// See: https://github.com/facebook/react-native/issues/49607
import 'react-native';

declare module 'react-native' {
  interface KeyboardAvoidingView {
    context: any;
    setState: any;
    forceUpdate: any;
    props: any;
    state: any;
    refs: any;
  }
}
